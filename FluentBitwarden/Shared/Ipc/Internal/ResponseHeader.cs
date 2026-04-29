using System.Buffers.Binary;

namespace FluentBitwarden.Shared.Ipc.Internal;

internal readonly record struct ResponseHeader(int PayloadLength)
{
    private const int HeaderSize = sizeof(ushort) + sizeof(int); // ProtocolVersion + PayloadLength

    private const int VersionOffset = 0;
    private const int PayloadLengthOffset = sizeof(ushort);

    public void Write(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];

        if (PayloadLength is <= 0 or > IpcConstants.MaxPayloadLength)
            throw new InvalidOperationException(
                $"Invalid payload length: {PayloadLength}.");

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.Slice(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            header.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        stream.Write(header);
    }
}
