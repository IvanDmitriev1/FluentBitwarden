namespace FluentBitwarden.ViewModels.Accounts.Login.Models;

internal static class LogIn2FProviderExtensions
{
    public static string GetTitle(this IdentityTwoFactorProviderType provider) => provider switch
    {
        IdentityTwoFactorProviderType.Authenticator => "Authenticator app",
        IdentityTwoFactorProviderType.Email => "Email code",
        IdentityTwoFactorProviderType.Duo => "Duo",
        IdentityTwoFactorProviderType.YubiKey => "YubiKey",
        IdentityTwoFactorProviderType.U2f => "U2F",
        _ => provider.ToString(),
    };
}