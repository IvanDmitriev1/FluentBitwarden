using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed partial class SecureNoteVaultCipherListItem : ListItem
{
    public SecureNoteVaultCipherListItem(
        SecureNoteVaultCipher cipher,
        IUiProcessManager uiProcessManager)
        : base(new OpenItemCommand(cipher, uiProcessManager))
    {
        Title = cipher.Name;
        Subtitle = "Secure note";
        Icon = Icons.SecureNote;
    }
}
