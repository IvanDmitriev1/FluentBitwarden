using System.Runtime;
using FluentBitwarden.Views.Shell;
using WinUIEx;

namespace FluentBitwarden.Application.Lifetime;

internal static class WindowManager
{
    private static MainWindow? _mainWindow;

    public static void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            _mainWindow = App.Current.GetRequiredService<MainWindow>();
            _mainWindow.Closed += (sender, args) =>
            {
                args.Handled = true;

                _mainWindow.IsShownInSwitchers = false;
                _mainWindow.Hide();
            };
        }

        _mainWindow.Activate();
        _mainWindow.Show();
        _mainWindow.BringToFront();
        _mainWindow.IsShownInSwitchers = true;
    }
}