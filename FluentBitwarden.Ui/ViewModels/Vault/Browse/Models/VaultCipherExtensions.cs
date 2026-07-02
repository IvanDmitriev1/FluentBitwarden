using System.Globalization;

namespace FluentBitwarden.ViewModels.Vault.Browse.Models;

public static class VaultCipherExtensions
{
    private const string MetadataDateFormat = "dddd, MMMM d, yyyy 'at' h:mm:ss tt";

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

    public static string LastEditedAtFormatted(this VaultCipher? cipher)
    {
        return cipher is null
            ? string.Empty
            : $"Last edited {FormatMetadataDate(cipher.RevisionDate)}";
    }

    public static string AddedAtFormatted(this VaultCipher? cipher)
    {
        return cipher is null
            ? string.Empty
            : $"Added {FormatMetadataDate(cipher.CreationDate)}";
    }

    private static string FormatMetadataDate(DateTimeOffset date)
    {
        return date
            .ToLocalTime()
            .ToString(MetadataDateFormat, CultureInfo.CurrentCulture);
    }
}
