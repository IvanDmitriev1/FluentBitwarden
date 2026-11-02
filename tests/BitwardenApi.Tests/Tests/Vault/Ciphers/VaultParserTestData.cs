using System.Buffers;
using System.Text.Json.Serialization;
using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Vault.Attachments.Responses;

namespace BitwardenApi.Tests.Tests.Vault.Ciphers;

internal static partial class VaultParserTestData
{
    internal static byte[] CreateLoginPayload(ReadOnlySpan<byte> key)
    {
        return WritePayload(key.ToArray(), static (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Synthetic item", keyBytes);
            WriteEncrypted(writer, "username", "synthetic-user", keyBytes);
            WriteEncrypted(writer, "password", "synthetic-password", keyBytes);
            writer.WriteEndObject();
        });
    }

    internal static byte[] CreateLoginPayloadWithUriAndFido2(ReadOnlySpan<byte> key)
    {
        return WritePayload(key.ToArray(), static (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Synthetic item", keyBytes);
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "uri", "https://example.com", keyBytes);
            writer.WriteNumber("match", (int)LoginUri.MatchType.Domain);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            WriteFido2Credential(writer, keyBytes, "12345678-1234-5678-9abc-def012345678");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
    }

    internal static void WriteFido2Credential(
        Utf8JsonWriter writer,
        ReadOnlySpan<byte> key,
        string credentialId,
        bool uppercase = false,
        bool includeCreationDate = true,
        bool nullCredentialId = false,
        bool nullCreationDate = false,
        bool includeUnknownMetadata = true,
        bool omitRpName = false)
    {
        writer.WriteStartObject();
        string credentialIdProperty = uppercase ? "CREDENTIALID" : "credentialId";
        if (nullCredentialId)
            writer.WriteNull(credentialIdProperty);
        else
            WriteEncrypted(writer, credentialIdProperty, credentialId, key);
        WriteEncrypted(writer, uppercase ? "KEYTYPE" : "keyType", "public-key", key);
        WriteEncrypted(writer, uppercase ? "KEYALGORITHM" : "keyAlgorithm", "ECDSA", key);
        WriteEncrypted(writer, uppercase ? "KEYCURVE" : "keyCurve", "P-256", key);
        WriteEncrypted(writer, uppercase ? "KEYVALUE" : "keyValue", "AQID", key);
        WriteEncrypted(writer, uppercase ? "RPID" : "rpId", "example.com", key);
        if (!omitRpName)
        {
            WriteEncrypted(writer, uppercase ? "RPNAME" : "rpName", "Example", key);
        }
        WriteEncrypted(writer, uppercase ? "USERHANDLE" : "userHandle", "AQID", key);
        WriteEncrypted(writer, uppercase ? "USERNAME" : "userName", "user", key);
        WriteEncrypted(writer, uppercase ? "USERDISPLAYNAME" : "userDisplayName", "User", key);
        WriteEncrypted(writer, uppercase ? "COUNTER" : "counter", "0", key);
        WriteEncrypted(writer, uppercase ? "DISCOVERABLE" : "discoverable", "false", key);
        if (includeCreationDate)
        {
            string creationDateProperty = uppercase ? "CREATIONDATE" : "creationDate";
            if (nullCreationDate)
                writer.WriteNull(creationDateProperty);
            else
                writer.WriteString(creationDateProperty, "2026-09-30T12:00:00Z");
        }
        if (includeUnknownMetadata)
        {
            writer.WriteStartObject(uppercase ? "UNKNOWNMETADATA" : "unknownMetadata");
            writer.WriteStartArray("nested");
            writer.WriteNumberValue(1);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    internal static void WriteLoginUri(
        Utf8JsonWriter writer,
        ReadOnlySpan<byte> key,
        string uri,
        int? match = null,
        string? uriChecksum = null,
        bool includeUnknownMetadata = true)
    {
        writer.WriteStartObject();
        WriteEncrypted(writer, "uri", uri, key);
        if (match is { } matchValue)
        {
            writer.WriteNumber("match", matchValue);
        }
        if (uriChecksum is not null)
        {
            writer.WriteString("uriChecksum", uriChecksum);
        }
        if (includeUnknownMetadata)
        {
            writer.WriteStartObject("unknownMetadata");
            writer.WriteBoolean("unsupported", true);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    internal static byte[] CreateUriMatchPayload(ReadOnlySpan<byte> key, Action<Utf8JsonWriter> writeMatch)
        => WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "uri", "https://match.example", keyBytes);
            writeMatch(writer);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

    internal static byte[] WritePayload(byte[] key, Action<Utf8JsonWriter, byte[]> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
            write(writer, key);

        return buffer.WrittenSpan.ToArray();
    }

    internal static CipherResponse CreateDto(CipherType type, byte[] payload) => new()
    {
        Id = CipherId.Parse("synthetic-cipher"),
        FolderId = FolderId.Parse("synthetic-folder"),
        VaultCipherType = type,
        RevisionDate = DateTimeOffset.Parse("2026-09-30T12:00:00Z"),
        CreationDate = DateTimeOffset.Parse("2026-09-29T12:00:00Z"),
        Favorite = true,
        Reprompt = false,
        Edit = true,
        ViewPassword = false,
        Data = payload
    };

    internal static AttachmentDownloadResponse CreateAttachment(
        string id,
        string fileName,
        long size,
        ReadOnlySpan<byte> key) => new()
    {
        Id = AttachmentId.Parse(id),
        Url = "https://example.invalid/attachment",
        EncryptedFileName = EncString.Encrypt(fileName, key),
        ProtectedAttachmentKey = EncString.Empty,
        Size = BitwardenApi.Primitives.FileSize.FromBytes(size)
    };

    internal static byte[] CreateEncryptedValuePayload(ReadOnlySpan<byte> key, string plaintext)
        => WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "value", plaintext, keyBytes);
            writer.WriteEndObject();
        });

    internal static void WriteEncrypted(Utf8JsonWriter writer, string propertyName, string value, ReadOnlySpan<byte> key)
    {
        writer.WritePropertyName(propertyName);
        JsonSerializer.Serialize(writer, EncString.Encrypt(value, key), VaultParserTestJsonContext.Default.EncString);
    }

    internal sealed class TestKey(byte[] keyBytes) : SymmetricCryptoKey(keyBytes);

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(EncString))]
    private partial class VaultParserTestJsonContext : JsonSerializerContext;
}
