using FluentBitwarden.Ui.Abstractions;

namespace FluentBitwarden.Ui;

public sealed class WindowHandleProvider : IWindowHandleProvider
{
    private nint _windowHandle;

    public bool TryGetWindowHandle(out nint windowHandle)
    {
        windowHandle = _windowHandle;
        return windowHandle != nint.Zero;
    }

    public void SetWindowHandle(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            throw new ArgumentException("Window handle must not be zero.", nameof(windowHandle));
        }

        _windowHandle = windowHandle;
    }
}
