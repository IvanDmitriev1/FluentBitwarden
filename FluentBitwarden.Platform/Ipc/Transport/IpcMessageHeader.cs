using CommunityToolkit.HighPerformance.Buffers;
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

    public ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);

        BinaryPrimitives.WriteUInt16LittleEndian(
            headerOwner.Span[VersionOffset..sizeof(ushort)],
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteUInt16LittleEndian(
            headerOwner.Span.Slice(MessageTypeOffset, sizeof(ushort)),
            MessageType);

        BinaryPrimitives.WriteInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        return stream.WriteAsync(headerOwner.Memory, cancellationToken);
    }

    public static async ValueTask<IpcMessageHeader> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);
        await stream.ReadExactlyAsync(headerOwner.Memory, cancellationToken);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            headerOwner.Span[VersionOffset..sizeof(ushort)]);

        var messageType = BinaryPrimitives.ReadUInt16LittleEndian(
            headerOwner.Span.Slice(MessageTypeOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)));

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
