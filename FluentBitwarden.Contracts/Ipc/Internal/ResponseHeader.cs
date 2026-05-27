using System.Buffers.Binary;

namespace FluentBitwarden.Contracts.Ipc.Internal;

internal readonly record struct ResponseHeader(int PayloadLength)
{
    private const int HeaderSize = sizeof(ushort) + sizeof(int); // ProtocolVersion + PayloadLength

    private const int VersionOffset = 0;
    private const int PayloadLengthOffset = sizeof(ushort);

    public static ResponseHeader Read(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        stream.ReadExactly(header);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            header.Slice(VersionOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            header.Slice(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");

        return new ResponseHeader(payloadLength);
    }

    public void Write(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.Slice(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            header.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        stream.Write(header);
    }
}
