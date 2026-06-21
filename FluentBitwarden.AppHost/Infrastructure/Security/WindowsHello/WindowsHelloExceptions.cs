using System.Security.Cryptography;

namespace FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;

public sealed class WindowsHelloAuthenticationCanceledException()
    : CryptographicException("Windows Hello authentication was cancelled.");

public sealed class WindowsHelloKeyUnavailableException()
    : CryptographicException("Windows Hello unlock is not available for this account.");
