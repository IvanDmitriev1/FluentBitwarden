using BitwardenApi.Models;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal static class BrowserLoginUriMatcher
{
    public static bool Matches(LoginVaultCipher cipher, BrowserPageUri pageUri) =>
        cipher.Uris.Any(uri => Matches(uri, pageUri));

    private static bool Matches(string savedUri, BrowserPageUri pageUri) =>
        VaultLoginUri.TryCreate(savedUri, out var loginUri) && loginUri.Matches(pageUri);
}
