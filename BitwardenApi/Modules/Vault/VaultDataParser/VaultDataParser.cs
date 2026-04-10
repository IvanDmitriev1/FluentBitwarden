using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    private delegate bool CipherPropertyReader<in T>(ref Utf8JsonReader reader, T cipher, scoped ReadOnlySpan<byte> key) where T : Cipher;
    private delegate T JsonArrayItemReader<out T>(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key);

    private static Utf8JsonReader CreateObjectReader(ReadOnlySpan<byte> payload)
    {
        var reader = new Utf8JsonReader(payload, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object payload.");
        }

        return reader;
    }

    private static T ParseCipherObject<T>(T cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key,
        CipherPropertyReader<T> readProperty) where T : Cipher
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (TryReadCommonCipherProperty(ref reader, cipher, key))
                continue;

            if (!readProperty.Invoke(ref reader, cipher, key))
                SkipValue(ref reader);
        }

        return cipher;
    }

    private static void SkipValue(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            reader.Skip();
    }

    private static string? ReadDecryptField(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return CryptographyService.DecryptString(ref reader, key);
    }

    private static List<T> ReadJsonArray<T>(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        JsonArrayItemReader<T> readItem)
    {
        reader.Read();

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected a JSON array");
        }

        var items = new List<T>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            items.Add(readItem(ref reader, key));
        }

        return items;
    }
}
