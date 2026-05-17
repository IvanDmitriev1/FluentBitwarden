using System.Buffers.Binary;
using System.Text;

namespace FluentBitwarden.Infrastructure.Ipc.Internal;

internal ref struct IpcPayloadReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _offset;

    public void ReadSchemaVersion(ushort expected, string payloadName)
    {
        ushort actual = ReadUInt16();
        if (actual != expected)
            throw new InvalidOperationException(
                $"Unsupported {payloadName} schema version: {actual}.");
    }

    private ushort ReadUInt16()
    {
        ReadOnlySpan<byte> bytes = ReadRaw(sizeof(ushort));
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    private uint ReadUInt32()
    {
        ReadOnlySpan<byte> bytes = ReadRaw(sizeof(uint));
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    public byte[] ReadByteArray()
    {
        ReadOnlySpan<byte> bytes = ReadRaw(ReadLength());
        return bytes.ToArray();
    }

    public string ReadString()
    {
        ReadOnlySpan<byte> bytes = ReadRaw(ReadLength());
        return Encoding.UTF8.GetString(bytes);
    }

    public void EnsureConsumed()
    {
        if (_offset != _buffer.Length)
            throw new InvalidOperationException("Unexpected trailing IPC payload bytes.");
    }

    private int ReadLength()
    {
        uint length = ReadUInt32();
        if (length > int.MaxValue)
            throw new InvalidOperationException($"IPC field length too large: {length} bytes.");

        return (int)length;
    }

    private ReadOnlySpan<byte> ReadRaw(int length)
    {
        if ((uint)length > (uint)(_buffer.Length - _offset))
            throw new InvalidOperationException("IPC payload ended before the current field was complete.");

        ReadOnlySpan<byte> bytes = _buffer.Slice(_offset, length);
        _offset += length;
        return bytes;
    }
}
