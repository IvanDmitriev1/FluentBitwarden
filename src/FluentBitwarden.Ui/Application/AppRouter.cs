using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Windows.Abstractions;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Application;

internal sealed class AppRouter(IWindowManager windowManager) : IAppRouter
{
    public void Activate(UiActivationCommand command, AppSessionStateSnapshot? state)
    {
        if (command is UiActivationCommand.OverlayCommand && state is not AppSessionStateSnapshot.NotAuthenticated)
            ActivateWindow(WindowKind.Overlay, state);
        else
            ActivateWindow(WindowKind.Main, state);
    }

    public void Navigate(AppSessionStateSnapshot? state)
    {
        if (windowManager.ActiveWindow is not { } windowKind)
            return;

        if (windowKind == WindowKind.Overlay && state is AppSessionStateSnapshot.NotAuthenticated)
        {
            ActivateWindow(WindowKind.Main, state);
            return;
        }

        RouteWindow(windowKind, state);
        if (windowKind == WindowKind.Overlay && state is AppSessionStateSnapshot.Unlocked)
            windowManager.CloseActiveWindow();
    }

    private void ActivateWindow(WindowKind windowKind, AppSessionStateSnapshot? state)
    {
        if (windowManager.EnsureWindow(windowKind))
            RouteWindow(windowKind, state);
    }

    private void RouteWindow(WindowKind windowKind, AppSessionStateSnapshot? state)
    {
        if (windowKind == WindowKind.Main)
            NavigateMainWindow(state);
        else
            NavigateOverlayWindow(state);
    }

    private void NavigateMainWindow(AppSessionStateSnapshot? state)
    {
        INavigation navigation = windowManager.WindowNavigation;
        switch (state)
        {
            case null:
                navigation.Navigate<LoadingPage>(NavigationKind.Reset);
                break;
            case AppSessionStateSnapshot.NotAuthenticated:
                navigation.Navigate<SetupPage>(NavigationKind.Reset);
                break;
            case AppSessionStateSnapshot.Locked locked:
                navigation.Navigate<UnlockPage, AccountProfile>(locked.Account, NavigationKind.Reset);
                break;
            case AppSessionStateSnapshot.Unlocked unlocked:
                navigation.Navigate<ShellPage, AccountProfile>(unlocked.Account, NavigationKind.Reset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

    }

    private void NavigateOverlayWindow(AppSessionStateSnapshot? state)
    {
        INavigation navigation = windowManager.WindowNavigation;
        switch (state)
        {
            case null:
            case AppSessionStateSnapshot.Unlocked:
                navigation.Navigate<LoadingPage>(NavigationKind.Reset);
                break;
            case AppSessionStateSnapshot.Locked locked:
                navigation.Navigate<UnlockPage, AccountProfile>(locked.Account, NavigationKind.Reset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

    }
}
