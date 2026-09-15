using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Views.Accounts;
using FluentBitwarden.Views.Shell;

namespace FluentBitwarden.ViewModels.Startup;

public sealed class LoadingPageViewModel(
    INavigation navigation,
    IAppCoordinator appCoordinator,
    IWindowManager windowManager) : ObservableObject, INavigationAware
{
    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        appCoordinator.SessionStateApplied += OnSessionStateApplied;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        appCoordinator.SessionStateApplied -= OnSessionStateApplied;
        return ValueTask.CompletedTask;
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
            case AppSessionState.Unlocked when windowManager.ActiveMode == WindowMode.Main:
                if (openIntent is null)
                {
                    navigation.Root.Navigate<ShellPage>(NavigationKind.Reset);
                }
                else
                {
                    navigation.Root.Navigate<ShellPage, OpenVaultCipherIntent>(
                        openIntent,
                        NavigationKind.Reset);
                }

                break;
        }
    }
}
