using FluentBitwarden.Shared.Ipc.Models;
using System.Buffers.Binary;

namespace FluentBitwarden.Shared.Ipc.Internal;

internal readonly record struct PipeHeader(
    ushort MessageType,
    int PayloadLength)
{
    private const int HeaderSize = 2 * sizeof(UInt16) + sizeof(int); // ProtocolVersion + MessageType + PayloadLength

    private const int VersionOffset = 0;
    private const int MessageTypeOffset = sizeof(ushort);
    private const int PayloadLengthOffset = sizeof(ushort) * 2;

    public static PipeHeader Read(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];

        if (header.Length != HeaderSize)
            throw new ArgumentException("Invalid IPC header size.", nameof(header));

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

        return new PipeHeader(messageType, payloadLength);
    }

    public void Write(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];

        if (header.Length != HeaderSize)
            throw new ArgumentException("Invalid IPC header size.", nameof(header));

        if (PayloadLength is <= 0 or > IpcConstants.MaxPayloadLength)
            throw new InvalidOperationException(
                $"Invalid payload length: {PayloadLength}.");

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.Slice(VersionOffset, sizeof(ushort)),
            IpcConstants.ProtocolVersion);

        BinaryPrimitives.WriteUInt16LittleEndian(
            header.Slice(MessageTypeOffset, sizeof(ushort)),
            MessageType);

        BinaryPrimitives.WriteInt32LittleEndian(
            header.Slice(PayloadLengthOffset, sizeof(int)),
            PayloadLength);

        stream.Write(header);
    }
}
