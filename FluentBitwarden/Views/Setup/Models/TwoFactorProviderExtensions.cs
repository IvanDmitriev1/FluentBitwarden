using BitwardenApi.Modules.Identity.Models;
using System.Text.Json;

namespace FluentBitwarden.Views.Setup.Models;

internal static class TwoFactorProviderExtensions
{
    public static string GetTitle(this TwoFactorProviderType provider) => provider switch
    {
        TwoFactorProviderType.Authenticator => "Authenticator app",
        TwoFactorProviderType.Email => "Email code",
        TwoFactorProviderType.Duo => "Duo",
        TwoFactorProviderType.YubiKey => "YubiKey",
        TwoFactorProviderType.U2f => "U2F",
        _ => provider.ToString(),
    };

    public static bool TryGetMetadataDisplayValue(this TwoFactorProviderOption provider, out string? value)
    {
        if (!provider.Metadata.TryGetValue("Email", out JsonElement metadataElement)
            && !provider.Metadata.TryGetValue("email", out metadataElement))
        {
            value = null;
            return false;
        }

        value = metadataElement.ValueKind switch
        {
            JsonValueKind.String => metadataElement.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => metadataElement.ToString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => metadataElement.ToString(),
        };

        return true;
    }
}