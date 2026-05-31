using BitwardenApi.Models;

namespace FluentBitwarden.Views.Accounts.LogIn.Models;

public sealed record LogIn2FProviderOptionModel(
    TwoFactorProviderType Provider,
    string Title,
    string Subtitle,
    bool IsSupported)
{

    public static LogIn2FProviderOptionModel CreateFrom(TwoFactorProviderOption provider) => new(
        provider.Provider,
        provider.Provider.GetTitle(),
        BuildSubtitle(provider),
        CheckIsSupported(provider.Provider));

    private static bool CheckIsSupported(TwoFactorProviderType provider) =>
        provider is TwoFactorProviderType.Authenticator or TwoFactorProviderType.Email;

    private static string BuildSubtitle(TwoFactorProviderOption provider)
    {
        if (provider.Provider == TwoFactorProviderType.Email
            && provider.TryGetMetadataDisplayValue(out string? emailHint)
            && !string.IsNullOrWhiteSpace(emailHint))
        {
            return emailHint;
        }

        return CheckIsSupported(provider.Provider)
            ? "Supported in this build"
            : "Not supported in this build";
    }
}