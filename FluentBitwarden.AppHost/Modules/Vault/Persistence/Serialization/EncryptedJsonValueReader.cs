using BitwardenApi.Vault.Cryptography;
using System.Security.Cryptography;
using System.Text.Json;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

internal static class EncryptedJsonValueReader
{
    private const int MaxStackByteCount = 256;

    public delegate T DecryptedJsonValueParser<out T>(scoped Span<byte> value);

    public static T ParseEncryptedValue<T>(
        this ref Utf8JsonReader reader,
        CipherKey key,
        DecryptedJsonValueParser<T> parser)
    {
        int length = reader.ValueSpan.Length;
        bool useStackAlloc = length <= MaxStackByteCount;

        Span<byte> buffer = useStackAlloc
            ? stackalloc byte[length]
            : new byte[length];

        int bytesWritten = reader.CopyString(buffer);

        try
        {
            bytesWritten = buffer[..bytesWritten].DecodeEncStringInPlace(key);
            return parser(buffer[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    public static T ReadRequired<T>(
        ref Utf8JsonReader reader,
        CipherKey key,
        DecryptedJsonValueParser<T> parser)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("Property must not be null.");

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Property must be an encrypted string.");

        return reader.ParseEncryptedValue(key, parser);
    }
}