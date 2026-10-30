using System.Text.Json.Serialization;

namespace BitwardenApi.Tests.Tests.Serialization;

public sealed class StronglyTypedIdJsonConverterTests
{
    private static readonly StronglyTypedIdJsonConverterTestsJsonContext Context =
        StronglyTypedIdJsonConverterTestsJsonContext.ConfiguredDefault;

    [Fact]
    public void Json_null_maps_to_default_for_string_and_guid_ids()
    {
        Assert.Equal(default, JsonSerializer.Deserialize("null", Context.ContextId));
        Assert.Equal(default, JsonSerializer.Deserialize("null", Context.OrganizationId));
    }

    [Fact]
    public void String_ids_preserve_normal_empty_and_literal_null_values()
    {
        ContextId normal = JsonSerializer.Deserialize("\"context-1\"", Context.ContextId);
        ContextId empty = JsonSerializer.Deserialize("\"\"", Context.ContextId);
        ContextId literalNull = JsonSerializer.Deserialize("\"null\"", Context.ContextId);

        Assert.Equal(ContextId.Parse("context-1"), normal);
        Assert.Equal(ContextId.Empty, empty);
        Assert.Equal(ContextId.Parse("null"), literalNull);
        Assert.Equal("\"null\"", JsonSerializer.Serialize(literalNull, Context.ContextId));
    }

    [Fact]
    public void Guid_ids_preserve_normal_and_empty_values_and_reject_empty_text()
    {
        const string id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        OrganizationId normal = JsonSerializer.Deserialize($"\"{id}\"", Context.OrganizationId);
        OrganizationId empty = JsonSerializer.Deserialize("\"00000000-0000-0000-0000-000000000000\"", Context.OrganizationId);

        Assert.Equal(OrganizationId.Parse(id), normal);
        Assert.Equal(OrganizationId.Empty, empty);
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize("\"\"", Context.OrganizationId));
    }

    [Fact]
    public void Default_id_serialization_matches_the_generated_converters()
    {
        Assert.Equal("null", JsonSerializer.Serialize(default(ContextId), Context.ContextId));
        Assert.Equal(
            "\"00000000-0000-0000-0000-000000000000\"",
            JsonSerializer.Serialize(default(OrganizationId), Context.OrganizationId));
    }

    [Fact]
    public void Dictionary_keys_use_the_generated_property_name_converters()
    {
        Dictionary<ContextId, string> stringIds = new()
        {
            [ContextId.Parse("context-1")] = "string value",
        };
        Dictionary<OrganizationId, string> guidIds = new()
        {
            [OrganizationId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")] = "guid value",
        };

        string stringJson = JsonSerializer.Serialize(stringIds, Context.ContextIdDictionary);
        string guidJson = JsonSerializer.Serialize(guidIds, Context.OrganizationIdDictionary);
        Dictionary<ContextId, string> stringRoundTrip = JsonSerializer.Deserialize(stringJson, Context.ContextIdDictionary)!;
        Dictionary<OrganizationId, string> guidRoundTrip = JsonSerializer.Deserialize(guidJson, Context.OrganizationIdDictionary)!;

        Assert.Equal("string value", stringRoundTrip[ContextId.Parse("context-1")]);
        Assert.Equal("guid value", guidRoundTrip[OrganizationId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]);
    }

    [Fact]
    public void Invalid_non_null_values_keep_the_generated_converter_failures()
    {
        AssertSameFailure(
            new ContextId.ContextIdSystemTextJsonConverter(),
            new ContextId.NullSafeSystemTextJsonConverter(),
            "42");
        AssertSameFailure(
            new OrganizationId.OrganizationIdSystemTextJsonConverter(),
            new OrganizationId.NullSafeSystemTextJsonConverter(),
            "\"not-a-guid\"");
        AssertSameFailure(
            new OrganizationId.OrganizationIdSystemTextJsonConverter(),
            new OrganizationId.NullSafeSystemTextJsonConverter(),
            "42");
    }

    private static void AssertSameFailure<T>(JsonConverter<T> original, JsonConverter<T> wrapped, string json)
    {
        Exception originalException = Assert.ThrowsAny<Exception>(() => Read(original, json));
        Exception wrappedException = Assert.ThrowsAny<Exception>(() => Read(wrapped, json));

        Assert.Equal(originalException.GetType(), wrappedException.GetType());
        Assert.Equal(originalException.Message, wrappedException.Message);
    }

    private static T Read<T>(JsonConverter<T> converter, string json)
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(utf8);
        Assert.True(reader.Read());
        return converter.Read(ref reader, typeof(T), new JsonSerializerOptions())!;
    }
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ContextId))]
[JsonSerializable(typeof(OrganizationId))]
[JsonSerializable(typeof(Dictionary<ContextId, string>), TypeInfoPropertyName = "ContextIdDictionary")]
[JsonSerializable(typeof(Dictionary<OrganizationId, string>), TypeInfoPropertyName = "OrganizationIdDictionary")]
internal sealed partial class StronglyTypedIdJsonConverterTestsJsonContext : JsonSerializerContext
{
    public static StronglyTypedIdJsonConverterTestsJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new ContextId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new OrganizationId.NullSafeSystemTextJsonConverter());
        return options;
    }
}
