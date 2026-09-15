using System.Buffers.Binary;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Internal;

internal ref struct SshAgentPayloadReader(ReadOnlyMemory<byte> buffer)
{
    private int _offset;

    public bool End => _offset == buffer.Length;
    public int Remaining => buffer.Length - _offset;

    public byte ReadByte()
    {
        if (Remaining < 1)
            throw new ArgumentOutOfRangeException(nameof(Remaining));

        byte value = buffer.Span[_offset];
        _offset++;
        return value;
    }

    public int ReadUInt32()
    {
        const int length = sizeof(uint);

        if (Remaining < length)
            throw new ArgumentOutOfRangeException(nameof(Remaining));

        uint value = BinaryPrimitives.ReadUInt32BigEndian(buffer.Span.Slice(_offset, length));

        _offset += length;
        return Convert.ToInt32(value);
    }

    public ReadOnlyMemory<byte> ReadString()
    {
        int count = ReadUInt32();
        if (Remaining < count)
            throw new ArgumentOutOfRangeException(nameof(Remaining));

        var value = buffer.Slice(_offset, count);
        _offset += count;
        return value;
    }
}