using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed partial class CardVaultCipherListItem : ListItem
{
    public CardVaultCipherListItem(
        CardVaultCipher cipher,
        IUiProcessManager uiProcessManager)
        : base(new OpenItemCommand(cipher, uiProcessManager))
    {
        Title = cipher.Name;
        Subtitle = string.IsNullOrWhiteSpace(cipher.Brand)
            ? "Card"
            : cipher.Brand;
        Icon = Icons.Card;
    }
}
