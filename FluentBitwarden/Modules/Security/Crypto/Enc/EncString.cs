using CommunityToolkit.HighPerformance.Buffers;
using System.Text.Json;

namespace FluentBitwarden.Modules.Security.Crypto.Enc;

internal sealed class EncString : IDisposable
{
    private MemoryOwner<char>? _owner;
    private readonly int _length;

    private EncString(MemoryOwner<char> owner, int length)
    {
        _owner = owner;
        _length = length;
    }

    public static EncString From(ReadOnlySpan<char> value)
    {
        MemoryOwner<char> owner = MemoryOwner<char>.Allocate(value.Length);
        value.CopyTo(owner.Span);
        return new EncString(owner, value.Length);
    }

    public static EncString From(string value) => From(value.AsSpan());

    internal static EncString FromJsonStringToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new InvalidOperationException("The current JSON token was not a string.");
        }

        int maxLength = checked((int)(reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length));
        MemoryOwner<char> owner = MemoryOwner<char>.Allocate(maxLength);
        int written = reader.CopyString(owner.Span);
        return new EncString(owner, written);
    }

    public EncStringParts Parse()
    {
        return !TryParse(out EncStringParts parsed)
            ? throw new FormatException($"The provided value '{ToString()}' is not a valid EncString.")
            : parsed;
    }

    public override string ToString() => new(AsSpan());

    public void Dispose()
    {
        MemoryOwner<char>? currentOwner = _owner;
        if (currentOwner is null)
        {
            return;
        }

        currentOwner.Span[.._length].Clear();
        currentOwner.Dispose();
        _owner = null;
    }

    public ReadOnlySpan<char> AsSpan()
    {
        MemoryOwner<char>? currentOwner = _owner;
        return currentOwner is null
            ? throw new ObjectDisposedException(nameof(EncString))
            : currentOwner.Span[.._length].Trim();
    }

    private bool TryParse(out EncStringParts parsed)
    {
        ReadOnlySpan<char> valueSpan = AsSpan();
        if (valueSpan.IsEmpty)
        {
            parsed = default;
            return false;
        }

        EncStringType type;
        ReadOnlySpan<char> payload;
        int typeSeparatorIndex = valueSpan.IndexOf('.');

        if (typeSeparatorIndex >= 0
            && int.TryParse(valueSpan[..typeSeparatorIndex].Trim(), out int typeValue))
        {
            type = (EncStringType)typeValue;
            payload = valueSpan[(typeSeparatorIndex + 1)..].Trim();
        }
        else
        {
            type = EncStringType.AesCbc256_B64;
            payload = valueSpan;
        }

        int firstSeparatorIndex = payload.IndexOf('|');
        int secondSeparatorIndex = firstSeparatorIndex < 0
            ? -1
            : payload[(firstSeparatorIndex + 1)..].IndexOf('|');

        ReadOnlySpan<char> firstSegment = firstSeparatorIndex < 0 ? payload : payload[..firstSeparatorIndex];
        ReadOnlySpan<char> secondSegment = firstSeparatorIndex < 0
            ? default
            : secondSeparatorIndex < 0
                ? payload[(firstSeparatorIndex + 1)..]
                : payload.Slice(firstSeparatorIndex + 1, secondSeparatorIndex);
        ReadOnlySpan<char> thirdSegment = secondSeparatorIndex < 0
            ? default
            : payload[(firstSeparatorIndex + secondSeparatorIndex + 2)..];

        if (secondSeparatorIndex >= 0 && thirdSegment.IndexOf('|') >= 0)
        {
            parsed = default;
            return false;
        }

        parsed = type switch
        {
            EncStringType.AesCbc256_B64 when firstSeparatorIndex >= 0 && secondSeparatorIndex < 0
                => new EncStringParts(type, secondSegment, firstSegment),
            EncStringType.AesCbc256_HmacSha256_B64 when firstSeparatorIndex >= 0 && secondSeparatorIndex >= 0
                => new EncStringParts(type, secondSegment, firstSegment, thirdSegment),
            EncStringType.Rsa2048_OaepSha1_B64 or EncStringType.Rsa2048_OaepSha256_B64 when firstSeparatorIndex < 0
                => new EncStringParts(type, firstSegment),
            EncStringType.Rsa2048_OaepSha1_HmacSha256_B64 or EncStringType.Rsa2048_OaepSha256_HmacSha256_B64
                when firstSeparatorIndex >= 0 && secondSeparatorIndex < 0
                => new EncStringParts(type, firstSegment, default, secondSegment),
            _ => default,
        };

        return !parsed.Data.IsEmpty;
    }
}
