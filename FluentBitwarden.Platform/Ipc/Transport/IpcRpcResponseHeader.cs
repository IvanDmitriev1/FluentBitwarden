using System.Buffers.Binary;
using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Platform.Ipc;

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
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);
        await stream.ReadExactlyAsync(headerOwner.Memory, cancellationToken);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            headerOwner.Span.Slice(VersionOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");

        if (payloadLength < 0)
            throw new InvalidDataException($"IPC payload length cannot be negative: {payloadLength}.");

        return new IpcRpcResponseHeader(payloadLength);
    }

    public ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);

        BinaryPrimitives.WriteUInt16LittleEndian(
            headerOwner.Span.Slice(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        return stream.WriteAsync(headerOwner.Memory, cancellationToken);
    }
}
