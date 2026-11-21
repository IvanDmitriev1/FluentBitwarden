using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.Extensions.Hosting;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Application;

internal sealed class AppRouter(IWindowManager windowManager, IHostApplicationLifetime lifetime) : IAppRouter
{
    public void Activate(UiActivationCommand command, AppSessionStateSnapshot? state)
    {
        if (lifetime.ApplicationStopping.IsCancellationRequested)
            return;

        if (windowManager.CurrentWindowKind is { } currentKind)
        {
            if (currentKind == WindowKind.Overlay && state is AppSessionStateSnapshot.NotAuthenticated)
                lifetime.StopApplication();
            else
                windowManager.EnsureWindow(currentKind);

            return;
        }

        if (command is not UiActivationCommand.OverlayCommand)
        {
            ActivateWindow(WindowKind.Main, state);
            return;
        }

        if (state is AppSessionStateSnapshot.NotAuthenticated)
            lifetime.StopApplication();
        else
            ActivateWindow(WindowKind.Overlay, state);
    }

    public void Navigate(AppSessionStateSnapshot? state)
    {
        if (lifetime.ApplicationStopping.IsCancellationRequested)
            return;

        if (windowManager.CurrentWindowKind is not { } kind)
            return;

        if (kind == WindowKind.Overlay && state is AppSessionStateSnapshot.NotAuthenticated)
        {
            lifetime.StopApplication();
            return;
        }

        RouteWindow(kind, state);
    }

    private void ActivateWindow(WindowKind kind, AppSessionStateSnapshot? state)
    {
        if (windowManager.EnsureWindow(kind))
            RouteWindow(kind, state);
    }

    private void RouteWindow(WindowKind kind, AppSessionStateSnapshot? state)
    {
        if (kind == WindowKind.Overlay)
            NavigateOverlayWindow(state);
        else
            NavigateMainWindow(state);
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
