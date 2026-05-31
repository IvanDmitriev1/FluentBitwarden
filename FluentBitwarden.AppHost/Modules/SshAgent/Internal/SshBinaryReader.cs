using System.Buffers.Binary;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Internal;

internal ref struct SshBinaryReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _offset = 0;

    public bool End => _offset == _buffer.Length;

    public ReadOnlySpan<byte> Remaining => _buffer[_offset..];

    public void ReadRaw(ReadOnlySpan<byte> expected)
    {
        ReadOnlySpan<byte> actual = ReadBytes(expected.Length);

        if (!actual.SequenceEqual(expected))
            throw new InvalidDataException("Unexpected binary data.");
    }

    public uint ReadUInt32()
    {
        ReadOnlySpan<byte> bytes = ReadBytes(sizeof(uint));
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    public ReadOnlySpan<byte> ReadString()
    {
        uint length = ReadUInt32();
        return ReadBytes(Convert.ToInt32(length));
    }

    public void ReadString(ReadOnlySpan<byte> expected)
    {
        ReadOnlySpan<byte> actual = ReadString();

        if (!actual.SequenceEqual(expected))
            throw new InvalidDataException("Unexpected string value.");
    }

    private ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (_buffer.Length - _offset < count)
            throw new EndOfStreamException("Unexpected end of SSH binary data.");

        ReadOnlySpan<byte> value = _buffer.Slice(_offset, count);
        _offset += count;
        return value;
    }
}