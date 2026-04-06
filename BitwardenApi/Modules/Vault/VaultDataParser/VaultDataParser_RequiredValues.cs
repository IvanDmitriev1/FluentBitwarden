using System.Globalization;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    private static string ReadRequiredDecryptField(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key, string propertyName)
    {
        return ReadDecryptField(ref reader, key)
            ?? throw new JsonException($"{propertyName} must not be null.");
    }

    private static int ReadRequiredEncryptedInt32(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key, string propertyName)
    {
        var value = ReadRequiredDecryptField(ref reader, key, propertyName);

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            throw new JsonException($"{propertyName} must be a valid Int32 value.");
        }

        return parsed;
    }

    private static bool ReadRequiredEncryptedBoolean(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key, string propertyName)
    {
        var value = ReadRequiredDecryptField(ref reader, key, propertyName);

        if (bool.TryParse(value, out bool parsed))
        {
            return parsed;
        }

        return value switch
        {
            "0" => false,
            "1" => true,
            _ => throw new JsonException($"{propertyName} must be a valid Boolean value.")
        };
    }

    private static DateTimeOffset ReadRequiredDateTimeOffset(ref Utf8JsonReader reader, string propertyName)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException($"{propertyName} must not be null.");
        }

        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTimeOffset(out var value))
        {
            throw new JsonException($"{propertyName} must be a valid DateTimeOffset value.");
        }

        return value;
    }
}
