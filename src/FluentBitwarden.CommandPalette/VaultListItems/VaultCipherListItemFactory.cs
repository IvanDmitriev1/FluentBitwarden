using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed class VaultCipherListItemFactory(
    ISiteIconCache siteIconCache,
    IUiProcessManager uiProcessManager)
{
    public IListItem Create(Cipher cipher) => cipher switch
    {
        LoginCipher login => new LoginVaultCipherListItem(login, siteIconCache, uiProcessManager),
        SecureNoteCipher secureNote => new SecureNoteVaultCipherListItem(secureNote, uiProcessManager),
        CardCipher card => new CardVaultCipherListItem(card, uiProcessManager),
        IdentityCipher identity => new IdentityVaultCipherListItem(identity, uiProcessManager),
        SshKeyCipher sshKey => new SshKeyVaultCipherListItem(sshKey, uiProcessManager),
        _ => throw new NotSupportedException($"Unsupported vault cipher type '{cipher.Type}'.")
    };
}
