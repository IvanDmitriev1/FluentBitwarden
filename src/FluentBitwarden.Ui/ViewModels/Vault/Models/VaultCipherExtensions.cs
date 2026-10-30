using System.Globalization;

namespace FluentBitwarden.ViewModels.Vault.Models;

public static class VaultCipherExtensions
{
    private const string MetadataDateFormat = "dddd, MMMM d, yyyy 'at' h:mm:ss tt";

    public static Uri? GetDefaultSiteIconUri(this Cipher? cipher)
    {
        if (cipher is not LoginCipher loginCipher)
            return null;

        return loginCipher.Uris.FirstOrDefault() is { } loginUri && loginUri.TryGetWebUri(out var uri)
            ? uri
            : null;
    }

    public static string? GetSubtitle(this Cipher? cipher) => cipher switch
    {
        CardCipher cardCipher => cardCipher.Brand,
        IdentityCipher identityCipher => identityCipher.Title,
        LoginCipher loginCipher => loginCipher.Username,
        _ => null
    };

    public static bool HasSubtitle(this Cipher cipher) => !string.IsNullOrEmpty(cipher.GetSubtitle());

    public static string GetDefaultGlyph(Cipher? cipher) => cipher switch
    {
        CardCipher => "\uE8C7",
        IdentityCipher => "\uE77B",
        SecureNoteCipher => "\uE70B",
        SshKeyCipher => "\uE192",
        _ => "\uE774"
    };

    public static string LastEditedAtFormatted(this Cipher? cipher)
    {
        return cipher is null
            ? string.Empty
            : $"Last edited {FormatMetadataDate(cipher.RevisionDate)}";
    }

    public static string AddedAtFormatted(this Cipher? cipher)
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
