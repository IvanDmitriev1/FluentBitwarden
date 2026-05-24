using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Views.Shell;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Implementations;

internal sealed class MainWindowHandleProvider : IWindowHandleProvider
{
    public nint GetWindowHandle() => MainWindow.Instance.GetWindowHandle();
}
