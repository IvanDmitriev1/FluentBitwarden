using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Infrastructure.Extensions;

public static class CipherExtensions
{
    public static string GetGlyph(this Cipher cipher) => cipher switch
    {
        CardCipher => "\uE8C7", // wallet/payment-ish, replace if you prefer another glyph
        IdentityCipher => "\uE77B", // contact/person
        SecureNoteCipher => "\uE70B", // note/document
        SshKeyCipher => "\uE192", // key
        _ => "\uE774" // globe/default
    };
}