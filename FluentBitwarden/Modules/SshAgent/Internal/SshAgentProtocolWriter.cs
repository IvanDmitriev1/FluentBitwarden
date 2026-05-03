using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Modules.SshAgent.Models;
using System.Buffers.Binary;
using System.Text;

namespace FluentBitwarden.Modules.SshAgent.Internal;

[Fody.ConfigureAwait(false)]
internal sealed class SshAgentProtocolWriter : IDisposable
{
    public SshAgentProtocolWriter(int payloadLength, SshAgentMessage message)
    {
        int packetLength = 4 + payloadLength;
        _owner = MemoryOwner<byte>.Allocate(packetLength);

       WriteUInt32(payloadLength);
       WriteByte((byte)message);
    }

    private readonly MemoryOwner<byte> _owner;
    private int _offset;
    private bool _isDisposed;

    private Span<byte> Span => _owner.Span;

    public static async Task WriteFailureAsync(Stream stream, CancellationToken ct)
    {
        using var writer = new SshAgentProtocolWriter(
            payloadLength: 1,
            message: SshAgentMessage.Failure);

        await writer.WriteToAsync(stream, ct);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _owner.Dispose();
        _isDisposed = true;
    }

    public ValueTask WriteToAsync(Stream stream, CancellationToken ct) =>
        stream.WriteAsync(_owner.Memory, ct);

    public void WriteUInt32(int value)
    {
        uint parsedValue = Convert.ToUInt32(value);
        const int length = sizeof(uint);
        EnsureRemaining(length);

        BinaryPrimitives.WriteUInt32BigEndian(Span.Slice(_offset, length), parsedValue);
        _offset += length;
    }

    public void WriteByte(byte value)
    {
        EnsureRemaining(1);

        _owner.Span[_offset] = value;
        _offset++;
    }

    public void WriteString(ReadOnlySpan<byte> value)
    {
        WriteUInt32(value.Length);
        EnsureRemaining(value.Length);

        value.CopyTo(_owner.Span[_offset..]);
        _offset += value.Length;
    }

    public void WriteString(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteUInt32(byteCount);
        EnsureRemaining(byteCount);

        int written = Encoding.UTF8.GetBytes(value, _owner.Span.Slice(_offset, byteCount));
        _offset += written;
    }

    private void EnsureRemaining(int length)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SshAgentProtocolWriter));

        if (_owner.Length - _offset < length)
            throw new InvalidOperationException("SSH agent response buffer is too small.");
    }
}