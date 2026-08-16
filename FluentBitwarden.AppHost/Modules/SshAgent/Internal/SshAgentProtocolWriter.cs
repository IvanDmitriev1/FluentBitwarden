using System.Buffers.Binary;
using System.Text;
using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Internal;

[Fody.ConfigureAwait(false)]
internal sealed class SshAgentProtocolWriter
{
    public SshAgentProtocolWriter(int payloadLength, SshAgentMessageReplies message)
    {
        int packetLength = 4 + payloadLength;
        _buffer = new byte[packetLength];

        WriteUInt32(payloadLength);
        WriteByte((byte)message);
    }

    private readonly byte[] _buffer;
    private int _offset;

    public Span<byte> Remaining => _buffer.AsSpan(_offset..);

    public static async Task WriteFailureAsync(Stream stream, CancellationToken ct)
    {
        var writer = new SshAgentProtocolWriter(
            payloadLength: 1,
            message: SshAgentMessageReplies.Failure);

        await writer.WriteToAsync(stream, ct);
    }

    public async ValueTask WriteToAsync(Stream stream, CancellationToken ct)
    {
        await stream.WriteAsync(_buffer, ct);
    }

    public void WriteUInt32(int value)
    {
        uint parsedValue = Convert.ToUInt32(value);
        const int length = sizeof(uint);
        EnsureRemaining(length);

        BinaryPrimitives.WriteUInt32BigEndian(Remaining, parsedValue);
        _offset += length;
    }

    public void WriteByte(byte value)
    {
        EnsureRemaining(1);

        _buffer[_offset] = value;
        _offset++;
    }

    public void WriteString(ReadOnlySpan<byte> value)
    {
        WriteUInt32(value.Length);
        EnsureRemaining(value.Length);

        value.CopyTo(Remaining);
        _offset += value.Length;
    }

    public void WriteString(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteUInt32(byteCount);
        EnsureRemaining(byteCount);

        int written = Encoding.UTF8.GetBytes(value, Remaining);
        _offset += written;
    }

    private void EnsureRemaining(int length)
    {
        if (_buffer.Length - _offset < length)
            throw new InvalidOperationException("SSH agent response buffer is too small.");
    }
}