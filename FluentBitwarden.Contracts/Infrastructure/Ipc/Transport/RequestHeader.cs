using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers.Binary;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Transport;

internal readonly record struct RequestHeader(
    ushort MessageType,
    int PayloadLength)
{
    private const int HeaderSize = 2 * sizeof(ushort) + sizeof(int); // ProtocolVersion + MessageType + PayloadLength

    private const int VersionOffset = 0;
    private const int MessageTypeOffset = sizeof(ushort);
    private const int PayloadLengthOffset = sizeof(ushort) * 2;

    public ValueTask WriteAsync(Stream stream)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);

        BinaryPrimitives.WriteUInt16LittleEndian(
            headerOwner.Span.Slice(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteUInt16LittleEndian(
            headerOwner.Span.Slice(MessageTypeOffset, sizeof(ushort)),
            MessageType);

        BinaryPrimitives.WriteInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        return stream.WriteAsync(headerOwner.Memory);
;    }

    public static async ValueTask<RequestHeader> ReadAsync(Stream stream)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);
        await stream.ReadExactlyAsync(headerOwner.Memory);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            headerOwner.Span.Slice(VersionOffset, sizeof(ushort)));

        var messageType = BinaryPrimitives.ReadUInt16LittleEndian(
            headerOwner.Span.Slice(MessageTypeOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");

        return new RequestHeader(messageType, payloadLength);
    }
}
