using FluentBitwarden.Application.Lifetime;
using WinUIEx;

namespace FluentBitwarden.Application.Tray;

internal class TrayIconService(IAppActivationService activationService, IAppRestartService appRestartService) : ITrayIconService
{
    private TrayIcon? _trayIcon;

    public void EnsureCreated()
    {
        if (_trayIcon is not null)
            return;

        _trayIcon = new TrayIcon(1, "Assets/Bitwarden_icon.ico", "FluentBitwarden");
        _trayIcon.IsVisible = true;

        _trayIcon.Selected += (_, _) => activationService.ReopenMainWindow();
        _trayIcon.LeftDoubleClick += (_, _) => activationService.ReopenMainWindow();
        _trayIcon.ContextMenu += OnContextMenu;
    }

    private void OnContextMenu(TrayIcon sender, TrayIconEventArgs args)
    {
        var flyout = new MenuFlyout();

        var showItem = new MenuFlyoutItem { Text = "Show" };
        showItem.Click += (_, _) => activationService.ReopenMainWindow();
        flyout.Items.Add(showItem);

        var lockItem = new MenuFlyoutItem { Text = "Lock" };
        lockItem.Click += (_, _) => appRestartService.RestartForLockAsync();
        flyout.Items.Add(lockItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (_, _) => App.Current.Exit();
        flyout.Items.Add(exitItem);

        args.Flyout = flyout;
    }
}