using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed partial class IdentityVaultCipherListItem : ListItem
{
    public IdentityVaultCipherListItem(
        IdentityVaultCipher cipher,
        IUiProcessManager uiProcessManager)
        : base(new OpenItemCommand(cipher, uiProcessManager))
    {
        Title = cipher.Name;
        Subtitle = GetSubtitle(cipher);
        Icon = Icons.Identity;
    }

    private static string GetSubtitle(IdentityVaultCipher cipher)
    {
        if (!string.IsNullOrWhiteSpace(cipher.Title))
            return cipher.Title;

        if (GetFullName(cipher) is { } fullName)
            return fullName;

        return string.IsNullOrWhiteSpace(cipher.Email)
            ? "Identity"
            : cipher.Email;
    }

    private static string? GetFullName(IdentityVaultCipher cipher)
    {
        var parts = new[]
        {
            cipher.FirstName,
            cipher.MiddleName,
            cipher.LastName
        };

        string fullName = string.Join(
            ' ',
            parts.Where(static part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName)
            ? null
            : fullName;
    }
}
