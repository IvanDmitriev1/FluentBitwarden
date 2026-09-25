using System.Security.Cryptography;

namespace FluentBitwarden.AppHost.Infrastructure.WindowsHelloIntegration;

public sealed class WindowsHelloAuthenticationCanceledException()
    : OperationCanceledException("Windows Hello authentication was cancelled.");

public sealed class WindowsHelloKeyUnavailableException()
    : CryptographicException("Windows Hello unlock is not available for this account.");
