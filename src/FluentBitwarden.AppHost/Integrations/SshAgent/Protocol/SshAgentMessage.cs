using System.Buffers.Binary;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal readonly record struct SshAgentMessage(
    SshAgentMessageType Type,
    ReadOnlyMemory<byte> Payload)
{
    public static readonly SshAgentMessage Failure =
        new SshAgentMessage(SshAgentMessageType.Failure, ReadOnlyMemory<byte>.Empty);

    public const int MaxLength = 1024 * 1024;

    private const int HeaderLength = 4;
    
    private readonly int _length = checked(1 + Payload.Length);


    public async ValueTask WriteMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (_length > MaxLength)
            throw new InvalidDataException("SSH agent response exceeds maximum size.");

        var frame = new byte[HeaderLength + _length];

        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, 4), (uint)_length);
        frame[4] = (byte)Type;
        Payload.CopyTo(frame.AsMemory(5));

        await stream.WriteAsync(frame, cancellationToken);
    }

    public static async ValueTask<SshAgentMessage> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] header = new byte[HeaderLength];
        int headerRead = await stream.ReadAtLeastAsync(
            header,
            header.Length,
            throwOnEndOfStream: false,
            cancellationToken: cancellationToken);

        if (headerRead != header.Length)
            return Failure;

        uint length = BinaryPrimitives.ReadUInt32BigEndian(
            header);

        if (length is 0 or > MaxLength)
            return Failure;

        byte[] body = new byte[(int)length];

        int bodyRead = await stream.ReadAtLeastAsync(
            body,
            body.Length,
            throwOnEndOfStream: false,
            cancellationToken: cancellationToken);

        if (bodyRead != body.Length)
            return Failure;

        return new SshAgentMessage((SshAgentMessageType)body[0], body.AsMemory(1));
    }
}
