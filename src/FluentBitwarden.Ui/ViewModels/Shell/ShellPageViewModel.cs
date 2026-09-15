using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Controls.VaultCiphers;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Views.Accounts;
using FluentBitwarden.Views.Startup;
using FluentBitwarden.Views.Vault;

namespace FluentBitwarden.ViewModels.Shell;

public sealed partial class ShellPageViewModel(
    INavigation navigation,
    IAppCoordinator appCoordinator,
    IWindowManager windowManager) : ObservableObject, INavigationAware, INavigationAware<OpenVaultCipherIntent>
{
    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        appCoordinator.SessionStateApplied += OnSessionStateApplied;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedToAsync(
        OpenVaultCipherIntent parameter,
        CancellationToken cancellationToken)
    {
        appCoordinator.SessionStateApplied += OnSessionStateApplied;
        OpenVaultCipher(parameter);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        appCoordinator.SessionStateApplied -= OnSessionStateApplied;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private void VaultCipherSelected(VaultCipherSearchBox.Selection selection)
    {
        navigation.Child!.Navigate<VaultPage, VaultCipherNavigationIntent>(
            new ShowVaultCipherIntent(selection.QueryText, selection.SelectedItem));
    }

    private void OpenVaultCipher(OpenVaultCipherIntent intent)
    {
        navigation.Child!.Navigate<VaultPage, VaultCipherNavigationIntent>(intent);
    }

    private void OnSessionStateApplied(
        AppSessionState state,
        UnlockPageParameter? unlockParameter,
        OpenVaultCipherIntent? openIntent)
    {
        switch (state)
        {
            case AppSessionState.LoggedOut:
                navigation.Root.Navigate<LogInFlowPage>(NavigationKind.Reset);
                break;
            case AppSessionState.Locked when unlockParameter is not null:
                navigation.Root.Navigate<UnlockPage, UnlockPageParameter>(
                    unlockParameter,
                    NavigationKind.Reset);
                break;
            case AppSessionState.Unlocked when windowManager.ActiveMode == WindowMode.Overlay:
                navigation.Root.Navigate<LoadingPage>(NavigationKind.Reset);
                break;
            case AppSessionState.Unlocked when openIntent is not null:
                OpenVaultCipher(openIntent);
                break;
        }
    }
}
