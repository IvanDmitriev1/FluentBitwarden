using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed partial class SshKeyVaultCipherListItem : ListItem
{
    public SshKeyVaultCipherListItem(
        SshKeyVaultCipher cipher,
        IUiProcessManager uiProcessManager)
        : base(new OpenItemCommand(cipher, uiProcessManager))
    {
        Title = cipher.Name;
        Subtitle = "SSH key";
        Icon = Icons.SshKey;
    }
}
