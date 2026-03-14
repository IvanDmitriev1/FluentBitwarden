using System.Text.Json;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Models.Vault;

internal enum EncStringType : byte
{
    AesCbc256_B64 = 0,
    AesCbc256_HmacSha256_B64 = 2,
    Rsa2048_OaepSha256_B64 = 4,
    Rsa2048_OaepSha1_B64 = 3,
    Rsa2048_OaepSha256_HmacSha256_B64 = 6,
    Rsa2048_OaepSha1_HmacSha256_B64 = 5,
}

internal readonly ref struct EncStringParts(
    EncStringType type,
    ReadOnlySpan<char> data,
    ReadOnlySpan<char> iv = default,
    ReadOnlySpan<char> mac = default)
{
    public EncStringType Type { get; } = type;
    public ReadOnlySpan<char> Data { get; } = data;
    public ReadOnlySpan<char> Iv { get; } = iv;
    public ReadOnlySpan<char> Mac { get; } = mac;
}

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

    public override string ToString()
        => new(AsSpan());

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
        if (currentOwner is null)
        {
            throw new ObjectDisposedException(nameof(EncString));
        }

        return currentOwner.Span[.._length].Trim();
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
        ReadOnlySpan<char> body;
        int headerSeparatorIndex = valueSpan.IndexOf('.');

        if (headerSeparatorIndex >= 0
            && int.TryParse(valueSpan[..headerSeparatorIndex].Trim(), out int typeValue))
        {
            type = (EncStringType)typeValue;
            body = valueSpan[(headerSeparatorIndex + 1)..].Trim();
        }
        else
        {
            type = EncStringType.AesCbc256_B64;
            body = valueSpan;
        }

        int firstSeparatorIndex = body.IndexOf('|');
        int secondSeparatorIndex = firstSeparatorIndex < 0
            ? -1
            : body[(firstSeparatorIndex + 1)..].IndexOf('|');

        ReadOnlySpan<char> first = firstSeparatorIndex < 0 ? body : body[..firstSeparatorIndex];
        ReadOnlySpan<char> second = firstSeparatorIndex < 0
            ? default
            : secondSeparatorIndex < 0
                ? body[(firstSeparatorIndex + 1)..]
                : body.Slice(firstSeparatorIndex + 1, secondSeparatorIndex);
        ReadOnlySpan<char> third = secondSeparatorIndex < 0
            ? default
            : body[(firstSeparatorIndex + secondSeparatorIndex + 2)..];

        if (secondSeparatorIndex >= 0 && third.IndexOf('|') >= 0)
        {
            parsed = default;
            return false;
        }

        parsed = type switch
        {
            EncStringType.AesCbc256_B64 when firstSeparatorIndex >= 0 && secondSeparatorIndex < 0
                => new EncStringParts(type, second, first),
            EncStringType.AesCbc256_HmacSha256_B64 when firstSeparatorIndex >= 0 && secondSeparatorIndex >= 0
                => new EncStringParts(type, second, first, third),
            EncStringType.Rsa2048_OaepSha1_B64 or EncStringType.Rsa2048_OaepSha256_B64 when firstSeparatorIndex < 0
                => new EncStringParts(type, first),
            EncStringType.Rsa2048_OaepSha1_HmacSha256_B64 or EncStringType.Rsa2048_OaepSha256_HmacSha256_B64
                when firstSeparatorIndex >= 0 && secondSeparatorIndex < 0
                => new EncStringParts(type, first, default, second),
            _ => default,
        };

        return !parsed.Data.IsEmpty;
    }
}
