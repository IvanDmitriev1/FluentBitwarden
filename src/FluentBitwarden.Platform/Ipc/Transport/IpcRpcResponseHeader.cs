using System.Buffers.Binary;

namespace FluentBitwarden.Platform.Ipc.Transport;

internal readonly record struct IpcRpcResponseHeader(int PayloadLength)
{
    private const int HeaderSize = sizeof(ushort) + sizeof(int); // ProtocolVersion + PayloadLength

    private const int VersionOffset = 0;
    private const int PayloadLengthOffset = sizeof(ushort);

    public static async ValueTask<IpcRpcResponseHeader> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[HeaderSize];
        await stream.ReadExactlyAsync(header, cancellationToken);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            header.AsSpan(VersionOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            header.AsSpan(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
        {
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");
        }

        if (payloadLength < 0)
            throw new InvalidDataException($"IPC payload length cannot be negative: {payloadLength}.");

        return new IpcRpcResponseHeader(payloadLength);
    }

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[HeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            header.AsSpan(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        await stream.WriteAsync(header, cancellationToken);
    }
}