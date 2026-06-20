using System.Text;
using System.Text.Json;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

public static partial class VaultDataParser
{
    private delegate bool CipherPropertyReader<in T>(ref Utf8JsonReader reader, T cipher, scoped ReadOnlySpan<byte> decryptKey) where T : VaultCipher;
    private delegate T JsonArrayItemReader<out T>(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey);

    private static Utf8JsonReader CreateObjectReader(ReadOnlySpan<byte> payload)
    {
        var reader = new Utf8JsonReader(payload, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object payload.");
        }

        return reader;
    }

    private static T ParseCipherObject<T>(T cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey,
        CipherPropertyReader<T> readProperty) where T : VaultCipher
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (TryReadCommonCipherProperty(ref reader, cipher, decryptKey))
                continue;

            if (!readProperty.Invoke(ref reader, cipher, decryptKey))
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

    private static string? ReadDecryptField(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return reader.ParseEncryptedValue(
            decryptKey,
            static value => Encoding.UTF8.GetString(value));
    }

    private static List<T> ReadJsonArray<T>(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> decryptKey,
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

            items.Add(readItem(ref reader, decryptKey));
        }

        return items;
    }
}
