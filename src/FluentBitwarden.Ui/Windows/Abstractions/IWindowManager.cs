using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Windows.Abstractions;

public interface IWindowManager : IThemeChangeable
{
    WindowKind? ActiveWindow { get; }

    NativeWindowHandle NativeWindowHandle { get; }
    INavigation WindowNavigation { get; }

    /// <summary>Ensures a window of the requested kind is open and active.</summary>
    /// <param name="kind">The kind of window to activate.</param>
    /// <returns><see langword="true"/> if a new window was created, including when it replaces the existing window. <see langword="false"/> if the existing window was reactivated.</returns>
    bool EnsureWindow(WindowKind kind);

    void CloseActiveWindow();
}
