using FluentBitwarden.Views.Shell;
using Microsoft.UI.Windowing;
using WinUIEx;

namespace FluentBitwarden.Application.Lifetime;

internal sealed class MainWindowService(MainWindow mainWindow) : IMainWindowService
{
    private bool _initialized;

    public void Show()
    {
        EnsureInitialized();

        mainWindow.AppWindow.IsShownInSwitchers = true;
        mainWindow.Activate();
        mainWindow.BringToFront();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        mainWindow.AppWindow.Closing += static (sender, args) =>
        {
            args.Cancel = true;
            sender.Hide();
        };

        var windowManager = WindowManager.Get(mainWindow);
        windowManager.WindowStateChanged += (_, state) =>
        {
            windowManager.AppWindow.IsShownInSwitchers = state != WindowState.Minimized;
        };

        if (mainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        _initialized = true;
    }
}
