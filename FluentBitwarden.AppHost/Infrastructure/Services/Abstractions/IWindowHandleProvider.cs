namespace FluentBitwarden.Infrastructure.Services.Abstractions;

public interface IWindowHandleProvider
{
    nint GetWindowHandle();
}
