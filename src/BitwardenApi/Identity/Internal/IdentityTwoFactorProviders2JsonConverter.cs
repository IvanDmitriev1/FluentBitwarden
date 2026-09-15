using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BitwardenApi.Identity.Internal;

internal sealed class IdentityTwoFactorProviders2JsonConverter : JsonConverter<IReadOnlyList<IdentityTwoFactorProviderOption>>
{
    public override IReadOnlyList<IdentityTwoFactorProviderOption> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("Two-factor providers list is null.");

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(
                $"Expected JSON object for TwoFactorProviders2, but got {reader.TokenType}.");
        }

        var providers = new List<IdentityTwoFactorProviderOption>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return providers;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected provider property name, but got {reader.TokenType}.");

            bool isSupportedProvider = TryParseIdentityTwoFactorProviderType(
                ref reader,
                out var providerType);

            // Move to metadata value.
            // It can be null or an object. For now we intentionally discard it.
            if (!reader.Read())
            {
                throw new JsonException(
                    "Unexpected end of JSON while reading provider metadata.");
            }

            reader.Skip();

            if (!isSupportedProvider)
                continue;

            providers.Add(new IdentityTwoFactorProviderOption(providerType!.Value));
        }

        throw new JsonException("Unexpected end of JSON while reading TwoFactorProviders2.");
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<IdentityTwoFactorProviderOption> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    private static bool TryParseIdentityTwoFactorProviderType(ref Utf8JsonReader reader, [NotNullWhen(true)] out IdentityTwoFactorProviderType? provider)
    {
        Span<byte> buffer = stackalloc byte[4];
        reader.CopyString(buffer);

        if (!int.TryParse(buffer, out var providerValue) ||
            !Enum.IsDefined(typeof(IdentityTwoFactorProviderType), providerValue))
        {
            provider = null;
            return false;
        }

        provider = (IdentityTwoFactorProviderType)providerValue;
        return true;
    }
}
