using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using Microsoft.UI;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Windows.Abstractions;

public interface IWindowManager : IThemeChangeable
{
    WindowKind? CurrentWindowKind { get; }
    WindowId WindowId { get; }

    NativeWindowHandle NativeWindowHandle { get; }
    INavigation WindowNavigation { get; }

    /// <summary>Ensures a window of the requested kind is open and active.</summary>
    /// <param name="kind">The kind of window to activate.</param>
    /// <returns><see langword="true"/> if a new window was created; otherwise, <see langword="false"/> if the existing window was reactivated.</returns>
    bool EnsureWindow(WindowKind kind);

}
