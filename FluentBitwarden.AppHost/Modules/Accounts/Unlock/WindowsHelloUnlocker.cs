using System.Security.Cryptography;
using FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;
using FluentBitwarden.AppHost.Modules.Accounts.Persistence;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed class WindowsHelloUnlocker(WindowsHelloKeyStore keyStore)
{
    public Task<bool> IsSupportedAsync() => WindowsHelloTpmKeyProtector.IsSupportedAsync();

    public void Enable(DecryptedUserKey decryptedKey, IntPtr hwnd)
    {
        var keyName = decryptedKey.UserId.ToString();

        WindowsHelloTpmKeyProtector.CreateOrReplaceWrappingKey(keyName, hwnd);

        byte[] protectedBytes = WindowsHelloTpmKeyProtector.WrapUserKey(
            keyName,
            decryptedKey.Key,
            hwnd);

        keyStore.Store(decryptedKey.UserId, protectedBytes);
    }

    public bool IsEnabled(UserId userId) => keyStore.Exists(userId);

    public void Disable(UserId userId) => RemoveWindowsHelloUnlock(userId);

    public AccountUnlockResult Unlock(AccountKeyMaterial accountKeyMaterial, IntPtr hwnd)
    {
        var userId = accountKeyMaterial.UserId;
        var keyName = userId.ToString();

        try
        {
            byte[]? protectedBytes = keyStore.Get(userId);

            if (protectedBytes is null)
            {
                AccountUnlockResult.WithoutUserKey(
                    new AccountUnlockOutcome.Failure(
                        "Windows Hello unlock is not enabled for this account. Unlock with your master password and enable Windows Hello again."));
            }

            byte[] decryptedBytes = WindowsHelloTpmKeyProtector.UnwrapUserKey(
                keyName,
                protectedBytes,
                hwnd);

            return AccountUnlockResult.WithUserKey(
                new AccountUnlockOutcome.Success(),
                new DecryptedUserKey(userId, decryptedBytes));
        }
        catch (WindowsHelloAuthenticationCanceledException)
        {
            return AccountUnlockResult.WithoutUserKey(
                new AccountUnlockOutcome.WindowsHelloCancelled());
        }
        catch (WindowsHelloKeyUnavailableException)
        {
            RemoveWindowsHelloUnlock(userId);

            return AccountUnlockResult.WithoutUserKey(
                new AccountUnlockOutcome.Failure(
                    "Windows Hello unlock is no longer available for this account. Unlock with your master password and enable Windows Hello again."));
        }
        catch (CryptographicException exception)
        {
            RemoveWindowsHelloUnlock(userId);
            return AccountUnlockResult.WithoutUserKey(new AccountUnlockOutcome.Failure(exception.Message));
        }
    }

    private void RemoveWindowsHelloUnlock(UserId userId)
    {
        WindowsHelloTpmKeyProtector.TryDeleteWrappingKey(userId.ToString());
        keyStore.Remove(userId);
    }
}
