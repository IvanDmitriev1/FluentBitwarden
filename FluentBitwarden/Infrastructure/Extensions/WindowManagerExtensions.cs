using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Views.Passkey;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Shell.Overlay;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace FluentBitwarden.Infrastructure.Extensions;

internal static class WindowManagerExtensions
{
    public static Window GetActiveWindow(this IWindowManager windowManager)
    {
        return windowManager.ActiveWindow
               ?? throw new InvalidOperationException("No FluentBitwarden window is active.");
    }

    public static IntPtr GetActiveWindowHandle(this IWindowManager windowManager)
    {
        return WindowNative.GetWindowHandle(windowManager.GetActiveWindow());
    }

    public static XamlRoot GetActiveXamlRoot(this IWindowManager windowManager)
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
