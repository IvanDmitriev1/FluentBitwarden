using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Abstractions;

public interface IWindowManager
{
    Window? ActiveWindow { get; }

    void SetWindow(WinUIEx.WindowEx window);
    void CloseWindow();
}
