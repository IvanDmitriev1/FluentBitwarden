namespace FluentBitwarden.ViewModels.Vault.Browse.Models;

public static class VaultCipherExtensions
{
    public static Uri? GetDefaultSiteIconUri(this VaultCipher? cipher)
    {
        if (cipher is not LoginVaultCipher loginCipher)
            return null;

        return loginCipher.Uris.FirstOrDefault() is { } loginUri && loginUri.TryGetWebUri(out var uri)
            ? uri
            : null;
    }

    public static string? GetSubtitle(this VaultCipher? cipher) => cipher switch
    {
        CardVaultCipher cardCipher => cardCipher.Brand,
        IdentityVaultCipher identityCipher => identityCipher.Title,
        LoginVaultCipher loginCipher => loginCipher.Username,
        _ => null
    };

    public static bool HasSubtitle(this VaultCipher cipher) => !string.IsNullOrEmpty(cipher.GetSubtitle());

    public static string GetDefaultGlyph(VaultCipher? cipher) => cipher switch
    {
        CardVaultCipher => "\uE8C7",
        IdentityVaultCipher => "\uE77B",
        SecureNoteVaultCipher => "\uE70B",
        SshKeyVaultCipher => "\uE192",
        _ => "\uE774"
    };
}
