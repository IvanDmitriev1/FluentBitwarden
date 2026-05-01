using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Views.Passkey;

public sealed partial class OverlayUnlockPage : Page
{
    public OverlayUnlockPage(
        StoredAccount selectedAccount,
        Action<DecryptedUserKey> onUnlock)
    {
        _onUnlock = onUnlock;
        SelectedAccount = selectedAccount;
        InitializeComponent();
    }

    private readonly Action<DecryptedUserKey> _onUnlock;

    public StoredAccount SelectedAccount { get; }

    [RelayCommand]
    private void VaultUnlockResult(UnlockResult result)
    {
        if (result is not UnlockResult.Success success)
            throw new InvalidOperationException("Failed to unlock the vault. (This should not happen.)");

        _onUnlock.Invoke(success.UserKey);
    }
}