using System.Buffers.Binary;
using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Contracts.Infrastructure.Ipc;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;

internal readonly record struct ResponseHeader(int PayloadLength)
{
    private const int HeaderSize = sizeof(ushort) + sizeof(int); // ProtocolVersion + PayloadLength

    private const int VersionOffset = 0;
    private const int PayloadLengthOffset = sizeof(ushort);

    public static async ValueTask<ResponseHeader> ReadAsync(Stream stream)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);
        await stream.ReadExactlyAsync(headerOwner.Memory);

        var version = BinaryPrimitives.ReadUInt16LittleEndian(
            headerOwner.Span.Slice(VersionOffset, sizeof(ushort)));

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)));

        if (version != IpcConstants.ProtocolVersion)
            throw new InvalidOperationException(
                $"Incompatible IPC version. Expected {IpcConstants.ProtocolVersion}, got {version}.");

        return new ResponseHeader(payloadLength);
    }

    public ValueTask Write(Stream stream)
    {
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderSize);

        BinaryPrimitives.WriteUInt16LittleEndian(
            headerOwner.Span.Slice(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            headerOwner.Span.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        return stream.WriteAsync(headerOwner.Memory);
    }
}
