using System.Security.Cryptography;
using System.Text.Json;
using BitwardenApi.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.Modules.Vault.Internal;

internal delegate T DecryptedJsonValueParser<out T>(scoped ReadOnlySpan<byte> value, string propertyName);

internal static class EncryptedJsonValueReader
{
    private const int MaxStackByteCount = 256;

    public static T ReadRequired<T>(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName,
        DecryptedJsonValueParser<T> parser)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException($"{propertyName} must not be null.");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{propertyName} must be an encrypted string.");
        }

        bool useStackAlloc = reader.ValueSpan.Length <= MaxStackByteCount;

        using var bufferOwner = useStackAlloc
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(reader.ValueSpan.Length);

        Span<byte> buffer = useStackAlloc
            ? stackalloc byte[reader.ValueSpan.Length]
            : bufferOwner.Span;

        try
        {
            int bytesWritten = CryptographyService.DecryptStringTo(ref reader, key, buffer);
            return parser(buffer[..bytesWritten], propertyName);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }
}
