using FluentBitwarden.Views.Shell;

namespace FluentBitwarden.Application.Lifetime;

internal static class WindowManager
{
    public static void ShowMainWindow()
    {
        var mainWindow = App.Current.GetRequiredService<MainWindow>();
        mainWindow.ShowWindow();
    }
}