using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.General;

namespace FluentBitwarden.Views.Passkey;

public sealed record UnlockPageParameter(IReadOnlyList<AccountProfile> Accounts, AccountProfile FavoriteAccountProfile);

public sealed partial class OverlayUnlockPage : Page
{
    public OverlayUnlockPage(
        AccountProfile selectedAccountProfile,
        Action onUnlock)
    {
        _onUnlock = onUnlock;
        SelectedAccountProfile = selectedAccountProfile;
        InitializeComponent();
    }

    private readonly Action _onUnlock;

    public AccountProfile SelectedAccountProfile { get; }

    [RelayCommand]
    private void VaultUnlockResult(AccountUnlockOutcome result)
    {
        if (result is not AccountUnlockOutcome.Success)
            throw new InvalidOperationException("Failed to unlock the vault. (This should not happen.)");

        _onUnlock.Invoke();
    }
}