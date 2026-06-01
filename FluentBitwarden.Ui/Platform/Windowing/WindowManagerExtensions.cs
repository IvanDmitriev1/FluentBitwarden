using FluentBitwarden.Views.Shell.Main;
using FluentBitwarden.Views.Shell.Overlay;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Platform.Windowing;

internal static class WindowManagerExtensions
{
    extension(IWindowManager windowManager)
    {
        public Window GetActiveWindow()
        {
            return windowManager.ActiveWindow
                   ?? throw new InvalidOperationException("No FluentBitwarden window is active.");
        }

        public XamlRoot GetActiveXamlRoot()
        {
            return windowManager.GetActiveWindow() switch
            {
                MainWindow mainWindow => mainWindow.XamlRoot,
                OverlayWindow overlayWindow => overlayWindow.XamlRoot,
                { Content: FrameworkElement content } => content.XamlRoot,
                _ => throw new InvalidOperationException("The active FluentBitwarden window does not expose a XamlRoot.")
            };
        }
    }
}
