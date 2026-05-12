using BitwardenApi.Models;

namespace FluentBitwarden.Infrastructure.Extensions;

public static class CipherExtensions
{
    public static string GetGlyph(this VaultCipher vaultCipher) => vaultCipher switch
    {
        CardVaultCipher => "\uE8C7", // wallet/payment-ish, replace if you prefer another glyph
        IdentityVaultCipher => "\uE77B", // contact/person
        SecureNoteVaultCipher => "\uE70B", // note/document
        SshKeyVaultCipher => "\uE192", // key
        _ => "\uE774" // globe/default
    };
}