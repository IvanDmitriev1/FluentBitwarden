using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

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
        bool nullCreationDate = false)
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
        WriteEncrypted(writer, uppercase ? "RPNAME" : "rpName", "Example", key);
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
        writer.WriteStartObject(uppercase ? "UNKNOWNMETADATA" : "unknownMetadata");
        writer.WriteStartArray("nested");
        writer.WriteNumberValue(1);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    internal static void WriteLoginUri(Utf8JsonWriter writer, ReadOnlySpan<byte> key, string uri)
    {
        writer.WriteStartObject();
        WriteEncrypted(writer, "uri", uri, key);
        writer.WriteStartObject("unknownMetadata");
        writer.WriteBoolean("unsupported", true);
        writer.WriteEndObject();
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

    internal static Cipher ParseCipher(
        ref readonly CipherResponse dto,
        ReadOnlySpan<byte> payload,
        SymmetricCryptoKey baseKey)
    {
        var parser = new FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser();
        return parser.ParseAndDecryptCipher(in dto, payload, baseKey);
    }

    internal static long MeasureAllocatedBytes(
        FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser parser,
        in CipherResponse dto,
        byte[] payload,
        SymmetricCryptoKey baseKey)
    {
        for (int i = 0; i < 32; i++)
        {
            _ = parser.ParseAndDecryptCipher(in dto, payload, baseKey);
        }

        const int iterations = 100;
        long startingBytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            _ = parser.ParseAndDecryptCipher(in dto, payload, baseKey);
        }

        return (GC.GetAllocatedBytesForCurrentThread() - startingBytes) / iterations;
    }

    internal static byte[] CreateEncryptedValuePayload(ReadOnlySpan<byte> key, string plaintext)
        => WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "value", plaintext, keyBytes);
            writer.WriteEndObject();
        });

    internal static T ParseEncryptedValueInPlace<T>(
        byte[] payload,
        ReadOnlySpan<byte> key,
        Span<byte> scratch,
        EncryptedJsonValueReader.DecryptedJsonValueParser<T> parser)
    {
        var reader = new Utf8JsonReader(payload);
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        return EncryptedJsonValueReader.ParseInPlace(ref reader, key, scratch, parser);
    }

    internal static List<LoginUri> ReadLoginUriArray(
        LoginUrisJsonConverter converter,
        ReadOnlySpan<byte> payload,
        JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(payload);
        Assert.True(reader.Read());
        return converter.Read(ref reader, typeof(List<LoginUri>), options);
    }

    internal static byte[] CreateDirtyScratch(int length) => Enumerable.Repeat((byte)0xA5, length).ToArray();

    internal static void AssertScratchCleared(byte[] scratch)
        => Assert.All(scratch, static value => Assert.Equal((byte)0, value));

    internal static long MeasureConverterAllocations<T>(
        DecryptedValueJsonConverter<T> converter,
        byte[] payload,
        JsonSerializerOptions options)
    {
        const int warmups = 32;
        const int iterations = 100;
        for (int i = 0; i < warmups; i++)
            _ = ReadConverterValue(converter, payload, options);

        // Shared-pool refills can contaminate a batch; measure steady-state allocations.
        long minimumBytes = long.MaxValue;
        for (int sample = 0; sample < 5; sample++)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
                _ = ReadConverterValue(converter, payload, options);

            minimumBytes = Math.Min(minimumBytes, GC.GetAllocatedBytesForCurrentThread() - start);
        }

        return minimumBytes / iterations;
    }

    internal static long MeasureUtf8StringAllocations(string plaintext)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(plaintext);
        const int warmups = 32;
        const int iterations = 100;
        for (int i = 0; i < warmups; i++)
            _ = Encoding.UTF8.GetString(utf8.AsSpan());

        long minimumBytes = long.MaxValue;
        for (int sample = 0; sample < 5; sample++)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
                _ = Encoding.UTF8.GetString(utf8.AsSpan());

            minimumBytes = Math.Min(minimumBytes, GC.GetAllocatedBytesForCurrentThread() - start);
        }

        return minimumBytes / iterations;
    }

    private static T ReadConverterValue<T>(
        DecryptedValueJsonConverter<T> converter,
        byte[] payload,
        JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(payload);
        reader.Read();
        reader.Read();
        reader.Read();
        return converter.Read(ref reader, typeof(T), options);
    }

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
