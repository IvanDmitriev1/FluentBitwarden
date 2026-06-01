using WinUIEx;

namespace FluentBitwarden.Platform.Windowing;

public interface IWindowManager
{
    WindowEx? ActiveWindow { get; }

    void SetWindow(WindowEx window);
    void CloseWindow();
}
