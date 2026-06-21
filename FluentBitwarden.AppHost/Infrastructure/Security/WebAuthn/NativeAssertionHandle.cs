using Windows.Win32.Security.Authentication.WebAuthn;

namespace FluentBitwarden.AppHost.Infrastructure.Security.WebAuthn;

internal sealed unsafe class NativeAssertionHandle : IDisposable
{
    public WEBAUTHN_ASSERTION* Value { get; private set; }

    public void Set(WEBAUTHN_ASSERTION* value)
    {
        if (Value is not null)
            PInvoke.WebAuthNFreeAssertion(Value);

        Value = value;
    }

    public void Dispose()
    {
        if (Value is not null)
        {
            PInvoke.WebAuthNFreeAssertion(Value);
            Value = null;
        }
    }
}