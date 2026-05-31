using WinUIEx;

namespace FluentBitwarden.Infrastructure.Abstractions;

public interface IWindowManager
{
    WindowEx? ActiveWindow { get; }

    void SetWindow(WindowEx window);
    void CloseWindow();
}
