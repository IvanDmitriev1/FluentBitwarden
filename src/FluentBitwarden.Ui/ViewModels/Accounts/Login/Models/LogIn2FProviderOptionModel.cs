namespace FluentBitwarden.ViewModels.Accounts.Login.Models;

public sealed record LogIn2FProviderOptionModel(
    IdentityTwoFactorProviderType Provider,
    string Title,
    string Subtitle,
    bool IsSupported)
{

    public static LogIn2FProviderOptionModel CreateFrom(IdentityTwoFactorProviderOption provider) => new(
        provider.Provider,
        provider.Provider.GetTitle(),
        BuildSubtitle(provider),
        CheckIsSupported(provider.Provider));

    private static bool CheckIsSupported(IdentityTwoFactorProviderType provider) =>
        provider is IdentityTwoFactorProviderType.Authenticator or IdentityTwoFactorProviderType.Email;

    private static string BuildSubtitle(IdentityTwoFactorProviderOption provider)
    {
        return CheckIsSupported(provider.Provider)
            ? "Supported in this build"
            : "Not supported in this build";
    }
}
