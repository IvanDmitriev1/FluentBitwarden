using System.Buffers;
using System.Text.Json.Serialization;

using BitwardenApi.Identity.Internal;
using BitwardenApi.Serialization;
using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Vault.Internal;
using TextEncoding = System.Text.Encoding;

namespace BitwardenApi.Tests.Tests.Serialization;

public sealed class HandwrittenJsonConverterTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("-2", true)]
    public void BooleanOrNumber_reads_boolean_and_integer_flags(string json, bool expected)
    {
        bool result = ReadJson(new BooleanOrNumberJsonConverter(), json);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void BooleanOrNumber_writes_json_booleans(bool value, string expectedJson)
    {
        Assert.Equal(expectedJson, WriteJson(new BooleanOrNumberJsonConverter(), value));
    }

    [Fact]
    public void BooleanOrNumber_rejects_non_boolean_non_numeric_tokens()
    {
        Assert.Throws<JsonException>(() => ReadJson(new BooleanOrNumberJsonConverter(), "\"true\""));
        Assert.Throws<JsonException>(() => ReadJson(new BooleanOrNumberJsonConverter(), "null"));
    }

    [Fact]
    public void StringToUtf8Bytes_reads_and_writes_utf8_text()
    {
        const string value = "café 🌙";
        var converter = new StringToUtf8BytesConverter();

        byte[] bytes = ReadJson(converter, "\"café 🌙\"");
        string outputJson = WriteJson(converter, TextEncoding.UTF8.GetBytes(value));

        Assert.Equal(TextEncoding.UTF8.GetBytes(value), bytes);
        Assert.Equal(value, JsonDocument.Parse(outputJson).RootElement.GetString());
    }

    [Fact]
    public void CipherResponse_data_reads_utf8_split_across_sequence_segments()
    {
        const string data = "café 🌙";
        const string json = "{\"id\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"type\":1,\"revisionDate\":\"2026-01-02T00:00:00Z\",\"creationDate\":\"2026-01-01T00:00:00Z\",\"favorite\":false,\"reprompt\":false,\"edit\":true,\"viewPassword\":true,\"data\":\"café 🌙\"}";
        byte[] utf8 = TextEncoding.UTF8.GetBytes(json);
        int split = Array.IndexOf(utf8, (byte)0xC3) + 1;
        var first = new JsonSequenceSegment(utf8.AsMemory(0, split));
        JsonSequenceSegment last = first.Append(utf8.AsMemory(split));
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
        Assert.True(reader.Read());

        CipherResponse response = JsonSerializer.Deserialize(
            ref reader,
            VaultJsonContext.ConfiguredDefault.CipherResponse)!;

        Assert.Equal(TextEncoding.UTF8.GetBytes(data), response.Data);
    }

    [Fact]
    public void StringToUtf8Bytes_requires_a_json_string()
    {
        Assert.Throws<JsonException>(() => ReadJson(new StringToUtf8BytesConverter(), "42"));
        Assert.Throws<JsonException>(() => ReadJson(new StringToUtf8BytesConverter(), "null"));
    }

    [Fact]
    public void Base64UrlByteArray_reads_and_writes_unpadded_url_safe_text()
    {
        var converter = new Base64UrlByteArrayJsonConverter();

        byte[] bytes = ReadJson(converter, "\"-_8\"");
        string outputJson = WriteJson(converter, [0xFB, 0xFF]);

        Assert.Equal(new byte[] { 0xFB, 0xFF }, bytes);
        Assert.Equal("\"-_8\"", outputJson);
    }

    [Fact]
    public void Base64UrlByteArray_accepts_empty_text_and_rejects_non_string_tokens()
    {
        Assert.Empty(ReadJson(new Base64UrlByteArrayJsonConverter(), "\"\""));
        Assert.Throws<JsonException>(() => ReadJson(new Base64UrlByteArrayJsonConverter(), "[]"));
    }

    [Fact]
    public void OptionalOrganizationId_maps_null_and_empty_values_to_empty_id()
    {
        var converter = new OptionalOrganizationIdJsonConverter();

        Assert.Equal(OrganizationId.Empty, ReadJson(converter, "null"));
        Assert.Equal(OrganizationId.Empty, ReadJson(converter, "\"\""));
        Assert.Equal("null", WriteJson(converter, OrganizationId.Empty));
        Assert.Equal(
            "\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"",
            WriteJson(converter, OrganizationId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
    }

    [Fact]
    public void OptionalFolderId_maps_null_and_empty_values_to_empty_id()
    {
        var converter = new OptionalFolderIdJsonConverter();

        Assert.Equal(FolderId.Empty, ReadJson(converter, "null"));
        Assert.Equal(FolderId.Empty, ReadJson(converter, "\"\""));
        Assert.Equal("null", WriteJson(converter, FolderId.Empty));
        Assert.Equal("\"folder\"", WriteJson(converter, FolderId.Parse("folder")));
    }

    [Fact]
    public void Optional_ids_reject_non_string_values()
    {
        Assert.Throws<JsonException>(() => ReadJson(new OptionalOrganizationIdJsonConverter(), "0"));
        Assert.Throws<JsonException>(() => ReadJson(new OptionalFolderIdJsonConverter(), "false"));
    }

    [Fact]
    public void CollectionIds_reads_null_empty_and_populated_entries()
    {
        var converter = new CollectionIdsJsonConverter();

        Assert.Empty(ReadJson(converter, "null"));
        Assert.Equal(
            [CollectionId.Parse("collection-1"), CollectionId.Empty, CollectionId.Empty],
            ReadJson(converter, "[\"collection-1\",null,\"\"]"));
    }

    [Fact]
    public void CollectionIds_writes_only_nonempty_ids()
    {
        var converter = new CollectionIdsJsonConverter();
        CollectionId[] ids = [CollectionId.Empty, CollectionId.Parse("collection-1"), CollectionId.Empty];

        Assert.Equal("[\"collection-1\"]", WriteJson(converter, ids));
    }

    [Fact]
    public void CollectionIds_rejects_wrong_shapes_and_unterminated_arrays()
    {
        var converter = new CollectionIdsJsonConverter();

        Assert.Throws<JsonException>(() => ReadJson(converter, "{}"));
        Assert.Throws<JsonException>(() => ReadJson(converter, "[1]"));
        Assert.ThrowsAny<JsonException>(() => ReadJson(converter, "["));
        Assert.Throws<JsonException>(() => ReadJson(converter, "[", isFinalBlock: false));
    }

    [Fact]
    public void TwoFactorProviders_reads_supported_keys_and_skips_unknown_keys_and_metadata()
    {
        IReadOnlyList<IdentityTwoFactorProviderOption> result = ReadJson(
            new TwoFactorProvidersJsonConverter(),
            "{\"0\":null,\"6\":{\"key\":[1,2]},\"7\":false,\"abc\":null}");

        Assert.Equal(
            [IdentityTwoFactorProviderType.Authenticator, IdentityTwoFactorProviderType.OrganizationDuo],
            result.Select(static provider => provider.Provider));
    }

    [Fact]
    public void TwoFactorProviders_reads_an_empty_object_and_rejects_null_or_other_roots()
    {
        var converter = new TwoFactorProvidersJsonConverter();

        Assert.Empty(ReadJson(converter, "{}"));
        Assert.Throws<JsonException>(() => ReadJson(converter, "null"));
        Assert.Throws<JsonException>(() => ReadJson(converter, "[]"));
    }

    [Fact]
    public void TwoFactorProviders_is_read_only()
    {
        Assert.Throws<NotSupportedException>(() => WriteJson(
            new TwoFactorProvidersJsonConverter(),
            Array.Empty<IdentityTwoFactorProviderOption>()));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"0\":")]
    public void TwoFactorProviders_rejects_incomplete_streamed_objects(string json)
    {
        Assert.Throws<JsonException>(() => ReadJson(
            new TwoFactorProvidersJsonConverter(), json, isFinalBlock: false));
    }

    private static T ReadJson<T>(JsonConverter<T> converter, string json, bool isFinalBlock = true)
    {
        byte[] utf8 = TextEncoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(utf8, isFinalBlock, state: default);
        Assert.True(reader.Read());
        return converter.Read(ref reader, typeof(T), new JsonSerializerOptions())!;
    }

    private static string WriteJson<T>(JsonConverter<T> converter, T value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            converter.Write(writer, value, new JsonSerializerOptions());

        return TextEncoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class JsonSequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public JsonSequenceSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public JsonSequenceSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new JsonSequenceSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = next;
            return next;
        }
    }
}
