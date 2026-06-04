using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Platform.Windowing;

public interface IWindowManager : IThemeChangeable
{
    bool HasWindow { get; }

    WindowEx ActiveWindow { get; }
    XamlRoot ActiveXamlRoot { get; }
    void SetWindow(WindowEx window);
    void CloseWindow();
}
