using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;
using FluentBitwarden.AppHost.Modules.Accounts.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed class WindowsHelloUnlocker(IUnitOfWorkFactory unitOfWorkFactory)
{
    public Task<bool> IsSupportedAsync() => WindowsHelloTpmKeyProtector.IsSupportedAsync();

    public void Enable(UserKey decryptedKey, IntPtr hwnd)
    {
        var keyName = decryptedKey.UserId.ToString();

        WindowsHelloTpmKeyProtector.CreateOrReplaceWrappingKey(keyName, hwnd);

        byte[] protectedBytes = WindowsHelloTpmKeyProtector.WrapUserKey(
            keyName,
            decryptedKey.Key,
            hwnd);

        using var unitOfWork = unitOfWorkFactory.Create();
        unitOfWork.WindowsHelloKeyStoreRepository.Store(decryptedKey.UserId, protectedBytes);
        unitOfWork.SaveChanges();
    }

    public bool IsEnabled(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.WindowsHelloKeyStoreRepository.Exists(userId);
    }

    public void Disable(UserId userId) => RemoveWindowsHelloUnlock(userId);

    public AccountUnlockResult Unlock(AccountKeyMaterial accountKeyMaterial, IntPtr hwnd)
    {
        var userId = accountKeyMaterial.UserId;
        var keyName = userId.ToString();

        try
        {
            using var unitOfWork = unitOfWorkFactory.Create();
            byte[]? protectedBytes = unitOfWork.WindowsHelloKeyStoreRepository.Get(userId);

            if (protectedBytes is null)
            {
                return AccountUnlockResult.WithoutUserKey(
                    new AccountUnlockOutcome.Failure(
                        "Windows Hello unlock is not enabled for this account. Unlock with your master password and enable Windows Hello again."));
            }

            byte[] decryptedBytes = WindowsHelloTpmKeyProtector.UnwrapUserKey(
                keyName,
                protectedBytes,
                hwnd);

            return AccountUnlockResult.WithUserKey(
                new AccountUnlockOutcome.Success(),
                new UserKey(userId, decryptedBytes));
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

        using var unitOfWork = unitOfWorkFactory.Create();
        unitOfWork.WindowsHelloKeyStoreRepository.Remove(userId);
        unitOfWork.SaveChanges();
    }
}
