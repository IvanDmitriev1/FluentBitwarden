using System.Buffers.Binary;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal ref struct SshAgentPayloadReader(ReadOnlyMemory<byte> buffer)
{
    private int _position;

    public readonly int Remaining => buffer.Length - _position;
    public readonly bool End => Remaining == 0;

    public bool TryReadUInt32(out uint value)
    {
        value = 0;

        if (Remaining < sizeof(uint))
            return false;

        value = BinaryPrimitives.ReadUInt32BigEndian(
            buffer.Span.Slice(_position, sizeof(uint)));

        _position += sizeof(uint);
        return true;
    }

    public bool TryReadString(out ReadOnlyMemory<byte> value)
    {
        value = default;

        if (Remaining < sizeof(uint))
            return false;

        uint length = BinaryPrimitives.ReadUInt32BigEndian(
            buffer.Span.Slice(_position, sizeof(uint)));

        if (length > (uint)(Remaining - sizeof(uint)))
            return false;

        _position += sizeof(uint);

        value = buffer.Slice(_position, (int)length);
        _position += (int)length;

        return true;
    }

    public ReadOnlyMemory<byte> ReadRemaining()
    {
        var remaining = buffer[_position..];
        _position = buffer.Length;

        return remaining;
    }
}
