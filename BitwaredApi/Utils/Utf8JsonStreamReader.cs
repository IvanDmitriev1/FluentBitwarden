using System.Text.Json;
using BitwaredApi.Extensions;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Utils;

internal sealed class Utf8JsonStreamReader(Stream stream, int initialBufferSize = 16 * 1024) : IDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private readonly ArrayPoolBufferWriter<byte> _bufferWriter = new(initialBufferSize);
    private int _bytesInBuffer;

    public bool IsFinalBlock { get; private set; }

    public bool HasBufferedData => _bytesInBuffer > 0;

    public ReadOnlySpan<byte> WrittenSpan => _bufferWriter.WrittenSpan;
    public ReadOnlyMemory<byte> WrittenMemory => _bufferWriter.WrittenMemory;

    public async ValueTask ReadMoreAsync(CancellationToken cancellationToken = default)
    {
        if (IsFinalBlock)
        {
            return;
        }

        Memory<byte> buffer = _bufferWriter.GetMemory(1);
        int read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        HandleBytesRead(read);
    }

    public void ReadMore()
    {
        if (IsFinalBlock)
        {
            return;
        }

        Span<byte> buffer = _bufferWriter.GetSpan(1);
        int read = _stream.Read(buffer);
        HandleBytesRead(read);
    }

    private void HandleBytesRead(int read)
    {
        if (read == 0)
        {
            IsFinalBlock = true;
            return;
        }

        _bufferWriter.Advance(read);
        _bytesInBuffer += read;
    }

    public Utf8JsonReader CreateReader()
        => new(_bufferWriter.WrittenSpan, IsFinalBlock, ReaderState);

    public void Advance(Utf8JsonReader reader)
        => Advance(reader.BytesConsumed, reader.CurrentState);

    public void Advance(long bytesConsumed, JsonReaderState readerState)
    {
        int consumed = checked((int)bytesConsumed);
        _bytesInBuffer -= consumed;
        _bufferWriter.CompactUnreadBytes(consumed, _bytesInBuffer);
        ReaderState = readerState;
    }

    public JsonReaderState ReaderState { get; private set; }

    public void Dispose()
        => _bufferWriter.Dispose();
}
