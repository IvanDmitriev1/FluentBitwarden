using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Shell;
using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;
using WinUIEx;

namespace FluentBitwarden.Modules.Security.Services.Unlock;

internal sealed class WindowsHelloSecurityService(
    IAccountSecurityRepository accountSecurityRepository,
    MainWindow mainWindow) : IWindowsHelloSecurityService
{
    public UnlockMethod Method => UnlockMethod.WindowsHello;

    public async ValueTask<WindowsHelloAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        bool keyCredentialAvailable = await KeyCredentialManager.IsSupportedAsync();
        if (!keyCredentialAvailable)
            return WindowsHelloAvailability.NotSupported;

        var result = await UserConsentVerifier.CheckAvailabilityAsync();

        return result switch
        {
            UserConsentVerifierAvailability.Available => WindowsHelloAvailability.Available,
            UserConsentVerifierAvailability.DeviceNotPresent => WindowsHelloAvailability.Available,
            UserConsentVerifierAvailability.NotConfiguredForUser => WindowsHelloAvailability.NotConfigured,
            UserConsentVerifierAvailability.DisabledByPolicy => WindowsHelloAvailability.Unavailable,
            UserConsentVerifierAvailability.DeviceBusy => WindowsHelloAvailability.Unavailable,
            _ => WindowsHelloAvailability.Unavailable,
        };
    }

    public async ValueTask<bool> EnableAsync(StoredAccount account, CancellationToken cancellationToken = default)
    {
        var keyCreationResult =
            await KeyCredentialManager.RequestCreateAsync(account.UserId.ToString(),
                KeyCredentialCreationOption.ReplaceExisting);

        var result = keyCreationResult.Status switch
        {
            KeyCredentialStatus.Success => true,
            _ => false
        };

        if (!result)
            return false;

        var security = await accountSecurityRepository.GetByAccountIdAsync(account.UserId, cancellationToken);
        ArgumentNullException.ThrowIfNull(security);

        await accountSecurityRepository.UpdateAsync(security with { HasWindowsHello = true }, cancellationToken);

        return true;
    }

    public ValueTask DisableAsync(StoredAccount account, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<UnlockResult> UnlockAsync(StoredAccount account, WindowsHelloUnlockRequest request,
        CancellationToken cancellationToken = default)
    {
        var availability = await GetAvailabilityAsync(cancellationToken);

        if (availability != WindowsHelloAvailability.Available)
            return new UnlockResult.Failure("Windows hello is not available");

        var windowHandle = mainWindow.GetWindowHandle();

        return new UnlockResult.Success(new UserKeySession(account.UserId, Method, []));
    }
}