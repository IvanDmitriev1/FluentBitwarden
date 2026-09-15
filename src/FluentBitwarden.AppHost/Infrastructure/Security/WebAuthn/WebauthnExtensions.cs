namespace FluentBitwarden.AppHost.Infrastructure.Security.WebAuthn;

internal static unsafe class WebAuthnExtensions
{
    private static void OpenPasskeySettings()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:privacy-passkeys",
            UseShellExecute = true
        });
    }

    public static void ThrowWebAuthnExceptionOnFailure(this HRESULT result)
    {
        if (!result.Failed)
        {
            return;
        }

        var exception = Marshal.GetExceptionForHR(result.Value)!;
        if (exception.HResult == -2147417829)
            OpenPasskeySettings();

        throw new WebAuthnLoginException($"Windows passkey authentication failed: {exception.Message}.", exception);
    }
}