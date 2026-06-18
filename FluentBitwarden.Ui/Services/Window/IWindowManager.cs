using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Services.Window;

public interface IWindowManager : IThemeChangeable
{
    bool HasWindow { get; }

    WindowEx ActiveWindow { get; }
    XamlRoot ActiveXamlRoot { get; }
    void SetWindow(WindowEx window);
    void CloseWindow();
}
