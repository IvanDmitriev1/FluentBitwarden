using System.Buffers.Binary;

namespace FluentBitwarden.Platform.Ipc.Transport;

internal readonly record struct IpcMessageHeader(
    ushort MessageType,
    int PayloadLength)
{
    private const int HeaderSize = 2 * sizeof(ushort) + sizeof(int); // ProtocolVersion + MessageType + PayloadLength

    private const int VersionOffset = 0;
    private const int MessageTypeOffset = sizeof(ushort);
    private const int PayloadLengthOffset = sizeof(ushort) * 2;

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[HeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(MessageTypeOffset, sizeof(ushort)),
            MessageType);

        BinaryPrimitives.WriteInt32LittleEndian(
            header.AsSpan(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        await stream.WriteAsync(header, cancellationToken);
    }

    public static async ValueTask<IpcMessageHeader> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[HeaderSize];
        await stream.ReadExactlyAsync(header, cancellationToken);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            header.AsSpan(VersionOffset, sizeof(ushort)));

        var messageType = BinaryPrimitives.ReadUInt16LittleEndian(
            header.AsSpan(MessageTypeOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            header.AsSpan(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
        {
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");
        }

        if (payloadLength < 0)
            throw new InvalidDataException($"IPC payload length cannot be negative: {payloadLength}.");

        return new IpcMessageHeader(messageType, payloadLength);
    }
}