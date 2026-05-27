namespace FluentBitwarden.AppHost.Infrastructure.Abstractions;

public interface IWindowHandleProvider
{
    nint GetWindowHandle();
}
