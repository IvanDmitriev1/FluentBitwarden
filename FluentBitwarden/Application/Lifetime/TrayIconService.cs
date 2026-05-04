using FluentBitwarden.Views.Shell;
using WinUIEx;

namespace FluentBitwarden.Application.Lifetime;

internal static class TrayIconService
{
    private static TrayIcon? _trayIcon;

    public static void EnsureCreated()
    {
        if (_trayIcon is not null)
            return;

        _trayIcon = new TrayIcon(1, "Assets/Bitwarden_icon.ico", "FluentBitwarden");
        _trayIcon.IsVisible = true;

        _trayIcon.Selected += (_, _) => WindowManager.ShowMainWindow();
        _trayIcon.LeftDoubleClick += (_, _) => WindowManager.ShowMainWindow();
        _trayIcon.ContextMenu += OnContextMenu;
    }

    private static void OnContextMenu(TrayIcon sender, TrayIconEventArgs args)
    {
        var flyout = new MenuFlyout();

        var showItem = new MenuFlyoutItem { Text = "Show" };
        showItem.Click += (_, _) => WindowManager.ShowMainWindow();
        flyout.Items.Add(showItem);

        var lockItem = new MenuFlyoutItem { Text = "Lock" };
        lockItem.Click += (_, _) => AppLifetimeManager.RestartApp();
        flyout.Items.Add(lockItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (_, _) =>
        {
            MainWindow.Instance.RequestExit();
        };
        flyout.Items.Add(exitItem);

        args.Flyout = flyout;
    }
}
