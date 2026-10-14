namespace FluentBitwarden.AppHost.Infrastructure.WebAuthn;

public sealed class WebAuthnLoginException(string message, Exception? innerException = null)
    : Exception(message, innerException);
