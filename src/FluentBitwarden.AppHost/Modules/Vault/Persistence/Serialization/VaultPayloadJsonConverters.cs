using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

internal sealed class DecryptedValueJsonConverter<T>(
    ReadOnlyMemory<byte> key,
    EncryptedJsonValueReader.DecryptedJsonValueParser<T> parseValue,
    bool allowNull = false) : JsonConverter<T>
{
    public ReadOnlyMemory<byte> Key { private get; set; } = key;

    public override bool HandleNull => true;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            if (allowNull)
                return default!;

            throw new JsonException("Encrypted value cannot be null.");
        }

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Encrypted value must be a string.");

        if (Key.IsEmpty)
            throw new InvalidOperationException("No active cipher key is available.");

        return reader.ParseEncryptedValue(Key.Span, parseValue);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => throw new NotSupportedException("Vault payload JSON is read-only.");
}

internal sealed class UriMatchTypeJsonConverter : JsonConverter<LoginUri.MatchType>
{
    public override bool HandleNull => true;

    public override LoginUri.MatchType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
            throw new JsonException("URI match must be a defined Int32 value.");

        var match = (LoginUri.MatchType)value;
        if (!Enum.IsDefined(match))
            throw new JsonException("URI match must be a defined Int32 value.");

        return match;
    }

    public override void Write(Utf8JsonWriter writer, LoginUri.MatchType value, JsonSerializerOptions options)
        => throw new NotSupportedException("Vault payload JSON is read-only.");
}

internal sealed class LoginUrisJsonConverter(
    Func<List<LoginUri>> getUris,
    JsonTypeInfo<LoginUri> uriTypeInfo) : JsonConverter<List<LoginUri>>
{
    public override bool HandleNull => true;

    public override List<LoginUri> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("URI list must be an array.");

        List<LoginUri> uris = getUris();
        uris.Clear();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return uris;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Each URI item must be a JSON object.");

            uris.Add(JsonSerializer.Deserialize(ref reader, uriTypeInfo)
                ?? throw new JsonException("URI item cannot be null."));
        }

        throw new JsonException("Incomplete URI list.");
    }

    public override void Write(Utf8JsonWriter writer, List<LoginUri> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Vault payload JSON is read-only.");
}

internal sealed class FirstFido2CredentialJsonConverter(
    JsonTypeInfo<Fido2Credential> credentialTypeInfo) : JsonConverter<Fido2Credential?>
{
    public override bool HandleNull => true;

    public override Fido2Credential? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("FIDO2 credentials must be an array or null.");

        if (!reader.Read())
            throw new JsonException("Incomplete FIDO2 credentials array.");

        if (reader.TokenType == JsonTokenType.EndArray)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Each FIDO2 credential must be a JSON object.");

        Fido2Credential first = JsonSerializer.Deserialize(ref reader, credentialTypeInfo)
            ?? throw new JsonException("FIDO2 credential cannot be null.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return first;

            reader.Skip();
        }

        throw new JsonException("Incomplete FIDO2 credentials array.");
    }

    public override void Write(Utf8JsonWriter writer, Fido2Credential? value, JsonSerializerOptions options)
        => throw new NotSupportedException("Vault payload JSON is read-only.");
}

internal static class VaultPayloadValueParsers
{
    public static string ParseString(Span<byte> value) => Encoding.UTF8.GetString(value);

    public static TotpValue? ParseTotp(Span<byte> value)
        => TotpValue.TryParse(value, out var totp) ? totp : null;

    public static OpenSshPublicKey ParseSshPublicKey(Span<byte> value)
    {
        string rawKey = Encoding.UTF8.GetString(value);
        return OpenSshPublicKey.TryParse(rawKey, out var publicKey)
            ? publicKey
            : OpenSshPublicKey.CreateUnparsed(rawKey);
    }

    public static T ParseNumber<T>(Span<byte> value) where T : IUtf8SpanParsable<T>
        => T.TryParse(value, provider: null, out T? parsed)
            ? parsed
            : throw new JsonException("Encrypted number is invalid.");

    public static bool ParseBoolean(Span<byte> value)
        => Ascii.EqualsIgnoreCase(value, "true"u8) || value.SequenceEqual("1"u8) ||
           (Ascii.EqualsIgnoreCase(value, "false"u8) || value.SequenceEqual("0"u8)
               ? false
               : throw new JsonException("Encrypted Boolean is invalid."));

    public static byte[] ParseBase64Url(Span<byte> value) => Base64Url.DecodeFromUtf8(value);

    public static byte[] ParseGuid(Span<byte> value)
        => Guid.TryParse(value, out Guid guid)
            ? guid.ToByteArray(bigEndian: true)
            : throw new JsonException("Encrypted GUID is invalid.");
}
