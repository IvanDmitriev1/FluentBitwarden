using System.Buffers.Binary;

namespace FluentBitwarden.Platform.Ipc.Transport;

internal readonly record struct IpcRpcResponseHeader(bool IsSuccessful, int PayloadLength)
{
    private const int HeaderSize = sizeof(ushort) + sizeof(byte) + sizeof(int); // ProtocolVersion + IsSuccessful + PayloadLength

    private const int VersionOffset = 0;
    private const int IsSuccessfulOffset = sizeof(ushort);
    private const int PayloadLengthOffset = sizeof(ushort) + sizeof(byte);

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

        byte isSuccessful = header[IsSuccessfulOffset];
        if (isSuccessful is not 0 and not 1)
            throw new InvalidDataException($"IPC response success flag must be 0 or 1, got {isSuccessful}.");

        if (payloadLength < 0)
            throw new InvalidDataException($"IPC payload length cannot be negative: {payloadLength}.");

        if (isSuccessful == 0 && payloadLength != 0)
        {
            throw new InvalidDataException(
                $"An unsuccessful IPC response must have an empty payload, got {payloadLength} bytes.");
        }

        return new IpcRpcResponseHeader(isSuccessful == 1, payloadLength);
    }

    public async ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[HeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        header[IsSuccessfulOffset] = IsSuccessful ? (byte)1 : (byte)0;

        if (!IsSuccessful && PayloadLength != 0)
        {
            throw new InvalidDataException("An unsuccessful IPC response must have an empty payload.");
        }

        BinaryPrimitives.WriteInt32LittleEndian(
            header.AsSpan(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        await stream.WriteAsync(header, cancellationToken);
    }
}
