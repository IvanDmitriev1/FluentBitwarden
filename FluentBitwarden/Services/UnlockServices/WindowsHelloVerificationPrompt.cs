using FluentBitwarden.Ui.Abstractions;
using Windows.Security.Credentials.UI;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class WindowsHelloVerificationPrompt(IWindowHandleProvider windowHandleProvider)
{
    private const int Windows11Build = 22000;

    public async ValueTask<bool> CanPromptAsync(CancellationToken cancellationToken = default)
    {
        if (!IsDesktopPromptSupported() || !windowHandleProvider.TryGetWindowHandle(out _))
        {
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await UserConsentVerifier.CheckAvailabilityAsync() == UserConsentVerifierAvailability.Available;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask VerifyAsync(string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!IsDesktopPromptSupported())
        {
            throw new InvalidOperationException("Windows Hello is not supported on this version of Windows.");
        }

        if (!windowHandleProvider.TryGetWindowHandle(out nint windowHandle))
        {
            throw new InvalidOperationException("Windows Hello is not available because the app window is not ready.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        UserConsentVerificationResult result;

        try
        {
            result = await UserConsentVerifierInterop.RequestVerificationForWindowAsync(windowHandle, message);
        }
        catch (TypeLoadException)
        {
            throw new InvalidOperationException("Windows Hello is not supported on this version of Windows.");
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException("Windows Hello is not supported on this version of Windows.");
        }

        if (result != UserConsentVerificationResult.Verified)
        {
            throw new InvalidOperationException(MapFailureMessage(result));
        }
    }

    private static bool IsDesktopPromptSupported()
        => OperatingSystem.IsWindowsVersionAtLeast(10, 0, Windows11Build);

    private static string MapFailureMessage(UserConsentVerificationResult result) => result switch
    {
        UserConsentVerificationResult.Canceled => "Windows Hello verification was canceled.",
        UserConsentVerificationResult.DeviceBusy => "Windows Hello is busy. Try again.",
        UserConsentVerificationResult.DeviceNotPresent => "Windows Hello is not available on this device.",
        UserConsentVerificationResult.DisabledByPolicy => "Windows Hello is disabled by policy.",
        UserConsentVerificationResult.NotConfiguredForUser => "Windows Hello is not configured for this user.",
        UserConsentVerificationResult.RetriesExhausted => "Windows Hello verification failed too many times. Try again.",
        _ => "Windows Hello verification failed.",
    };
}
