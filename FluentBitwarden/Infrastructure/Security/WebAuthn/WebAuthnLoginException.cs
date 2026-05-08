namespace FluentBitwarden.Infrastructure.Security.WebAuthn;

internal sealed class WebAuthnLoginException(string message, Exception? innerException = null)
    : Exception(message, innerException);
