using System.Buffers.Binary;

namespace FluentBitwarden.Infrastructure.Ipc.Internal;

internal readonly record struct RequestHeader(
    ushort MessageType,
    int PayloadLength)
{
    private const int HeaderSize = 2 * sizeof(ushort) + sizeof(int); // ProtocolVersion + MessageType + PayloadLength

    private const int VersionOffset = 0;
    private const int MessageTypeOffset = sizeof(ushort);
    private const int PayloadLengthOffset = sizeof(ushort) * 2;

    public static RequestHeader Read(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        stream.ReadExactly(header);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            header.Slice(VersionOffset, sizeof(ushort)));

        var messageType = BinaryPrimitives.ReadUInt16LittleEndian(
            header.Slice(MessageTypeOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            header.Slice(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");

        if (payloadLength is <= 0 or > IpcConstants.MaxPayloadLength)
            throw new InvalidOperationException(
                $"Invalid payload length: {payloadLength} bytes.");

        return new RequestHeader(messageType, payloadLength);
    }
}
