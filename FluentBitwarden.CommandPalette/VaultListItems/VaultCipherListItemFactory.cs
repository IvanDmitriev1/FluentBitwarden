using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed class VaultCipherListItemFactory(
    ISiteIconCache siteIconCache,
    IUiProcessManager uiProcessManager)
{
    public IListItem Create(VaultCipher cipher) => cipher switch
    {
        LoginVaultCipher login => new LoginVaultCipherListItem(login, siteIconCache, uiProcessManager),
        SecureNoteVaultCipher secureNote => new SecureNoteVaultCipherListItem(secureNote, uiProcessManager),
        CardVaultCipher card => new CardVaultCipherListItem(card, uiProcessManager),
        IdentityVaultCipher identity => new IdentityVaultCipherListItem(identity, uiProcessManager),
        SshKeyVaultCipher sshKey => new SshKeyVaultCipherListItem(sshKey, uiProcessManager),
        _ => throw new NotSupportedException($"Unsupported vault cipher type '{cipher.Type}'.")
    };
}
