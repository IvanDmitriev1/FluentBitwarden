using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Views.Passkey;

public sealed partial class OverlayUnlockPage : Page
{
    public OverlayUnlockPage(
        AccountProfile selectedAccountProfile,
        Action<DecryptedUserKey> onUnlock)
    {
        _onUnlock = onUnlock;
        SelectedAccountProfile = selectedAccountProfile;
        InitializeComponent();
    }

    private readonly Action<DecryptedUserKey> _onUnlock;

    public AccountProfile SelectedAccountProfile { get; }

    [RelayCommand]
    private void VaultUnlockResult(AccountUnlockOutcome result)
    {
        if (result is not AccountUnlockOutcome.Success success)
            throw new InvalidOperationException("Failed to unlock the vault. (This should not happen.)");

        _onUnlock.Invoke(success.UserKey);
    }
}