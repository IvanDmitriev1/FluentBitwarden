using WinUIEx;

namespace FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;

public interface IWindowManager
{
    WindowEx ActiveWindow { get; }

    void ReplaceWindow<TWindow>() where TWindow : WindowEx, new();
}
