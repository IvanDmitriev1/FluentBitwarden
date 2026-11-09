using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Ui.Screens.Shell.Models;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Shell;

public sealed partial class ShellViewModel(
    AccountProfile account,
    INavigation navigation) : ObservableObject
{
    public AccountProfile Account => account;

    [RelayCommand]
    private void VaultCipherSelected(VaultCipherSelection selection)
    {
        navigation.Navigate<VaultPage, VaultCipherNavigationIntent>(
            new ShowVaultCipherIntent(
                selection.QueryText,
                selection.SelectedItem));
    }
}
