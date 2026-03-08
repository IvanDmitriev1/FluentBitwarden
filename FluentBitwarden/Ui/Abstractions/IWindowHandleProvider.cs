namespace FluentBitwarden.Ui.Abstractions;

public interface IWindowHandleProvider
{
    bool TryGetWindowHandle(out nint windowHandle);
}
