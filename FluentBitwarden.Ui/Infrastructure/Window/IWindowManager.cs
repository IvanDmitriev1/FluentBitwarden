using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Window;

public interface IWindowManager : IThemeChangeable
{
    WindowMode ActiveMode { get; }
    IntPtr WindowHandle { get; }
    XamlRoot XamlRoot { get; }

    void ShowOrCreateWindow(WindowMode mode);
    void ReplaceWindow(WindowMode mode);
    void ActivateWindow();
    void MinimizeWindow();
    void CloseWindow();

    void ReplacePage<TPage>(IPageNavigationParameter? parameter = null) where TPage : Page;
}
