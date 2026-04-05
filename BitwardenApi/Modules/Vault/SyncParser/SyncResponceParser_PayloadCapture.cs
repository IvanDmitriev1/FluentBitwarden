using CommunityToolkit.HighPerformance.Buffers;
using System.Text.Json;

namespace BitwardenApi.Modules.Vault.SyncParser;

internal sealed class CipherPayloadCapture : IDisposable
{
    private MemoryOwner<byte> _payloadBuffer = MemoryOwner<byte>.Allocate(1024 * 4);
    private int _payloadLength;

    public bool HasCapturedPayload { get; private set; }

    public ReadOnlySpan<byte> PayloadSpan => _payloadBuffer.Span[.._payloadLength];

    public void Reset()
    {
        _payloadLength = 0;
        HasCapturedPayload = false;
    }

    public void CaptureDecodedStringPayload(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token for payload capture, got {reader.TokenType}.");
        }

        int maxLength = reader.HasValueSequence
            ? checked((int)reader.ValueSequence.Length)
            : reader.ValueSpan.Length;

        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (_payloadBuffer.Length < maxLength)
        {
            _payloadBuffer.Dispose();
            _payloadBuffer = MemoryOwner<byte>.Allocate(maxLength);
        }

        _payloadLength = reader.CopyString(_payloadBuffer.Span);
        HasCapturedPayload = true;
    }

    public void Dispose()
    {
        _payloadBuffer.Dispose();
    }
}
