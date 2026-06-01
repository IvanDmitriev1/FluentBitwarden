using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    private static string ReadRequiredDecryptField(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey,
        string propertyName) => ReadDecryptField(ref reader, decryptKey) ??
                                throw new JsonException($"{propertyName} must not be null.");

    private static int ReadRequiredEncryptedInt32(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
        => EncryptedJsonValueReader.ReadRequired(ref reader, decryptKey, static (scoped value) =>
        {
            if (Utf8Parser.TryParse(value, out int parsed, out int bytesConsumed) && bytesConsumed == value.Length)
            {
                return parsed;
            }

            throw new JsonException($"Property must be a valid Int32 value.");
        });

    private static bool ReadRequiredEncryptedBoolean(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
        => EncryptedJsonValueReader.ReadRequired(ref reader, decryptKey, static (scoped value) =>
        {
            if (Ascii.EqualsIgnoreCase(value, "true"u8) || value.SequenceEqual("1"u8))
            {
                return true;
            }

            if (Ascii.EqualsIgnoreCase(value, "false"u8) || value.SequenceEqual("0"u8))
            {
                return false;
            }

            throw new JsonException("Property must be a valid Boolean value.");
        });

    private static byte[] ReadBase64UrlBytes(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> decryptKey)
        => EncryptedJsonValueReader.ReadRequired(ref reader, decryptKey,
            static value => Base64Url.DecodeFromUtf8(value));

    private static DateTimeOffset ReadRequiredDateTimeOffset(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException($"Property must not be null.");
        }

        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTimeOffset(out var value))
        {
            throw new JsonException($"Property must be a valid DateTimeOffset value.");
        }

        return value;
    }
}
