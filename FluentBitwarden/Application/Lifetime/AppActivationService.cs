using FluentBitwarden.Views.Shell;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Application.Lifetime;

internal sealed class AppActivationService(MainWindow mainWindow) : IAppActivationService
{
    public void Activate(LaunchActivatedEventArgs args)
    {
        mainWindow.Activate();
        mainWindow.BringToFront();

        mainWindow.AppWindow.Closing += static (sender, args) =>
        {
            args.Cancel = true;
            sender.Hide();
        };

        var wm = WindowManager.Get(mainWindow);
        wm.WindowStateChanged += (s, state) =>
        {
            wm.AppWindow.IsShownInSwitchers = state != WindowState.Minimized;
        };

        if (mainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    public void ReopenMainWindow()
    {
        mainWindow.AppWindow.IsShownInSwitchers = true;
        mainWindow.Activate();
        mainWindow.BringToFront();
    }
}