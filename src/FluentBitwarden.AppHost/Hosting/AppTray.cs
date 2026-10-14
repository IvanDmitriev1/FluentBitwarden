using FluentBitwarden.AppHost.Infrastructure.Tray;

namespace FluentBitwarden.AppHost.Hosting;

internal sealed class AppTray(AppHostActions actions)
{
    public IReadOnlyList<TrayMenuItem> CreateMenu() =>
    [
        new TrayMenuItem.Command("Show", actions.ShowMainWindow),
        new TrayMenuItem.Command("Lock", actions.Lock),
        new TrayMenuItem.Separator(),
        new TrayMenuItem.Command("Exit", actions.RequestExit)
    ];

    public void Activate() => actions.ShowMainWindow();
}
