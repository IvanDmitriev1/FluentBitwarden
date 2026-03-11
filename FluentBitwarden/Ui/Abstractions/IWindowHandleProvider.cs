namespace FluentBitwarden.Ui.Abstractions;

/// <summary>
/// Exposes access to the current native window handle.
/// </summary>
public interface IWindowHandleProvider
{
    /// <summary>
    /// Tries to get the current window handle.
    /// </summary>
    bool TryGetWindowHandle(out nint windowHandle);
}
