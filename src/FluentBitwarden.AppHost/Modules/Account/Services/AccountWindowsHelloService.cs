using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Infrastructure.WindowsHelloIntegration;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Persistance;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountWindowsHelloService(
    IUnitOfWork unitOfWork,
    AccountTpmUnlockKeyRepository repository) : IAccountWindowsHelloService
{
    public Task<bool> IsSupportedAsync() => WindowsHelloTpmKeyProtector.IsSupportedAsync();

    public bool IsEnabled(UserId userId) => repository.Exists(userId);

    public void Enable(UnlockedUserKey userKey, IntPtr hwnd)
    {
        var keyName = userKey.UserId.ToString();

        WindowsHelloTpmKeyProtector.CreateOrReplaceWrappingKey(keyName, hwnd);

        byte[] protectedBytes = WindowsHelloTpmKeyProtector.WrapUserKey(
            keyName,
            userKey.Key,
            hwnd);

        unitOfWork.Begin();
        repository.Store(userKey.UserId, protectedBytes);
        unitOfWork.Commit();
    }

    public void Disable(UserId userId) => RemoveWindowsHelloUnlock(userId);

    public AccountKeyUnlockResult Unlock(AccountKeyMaterial accountKeyMaterial, IntPtr hwnd)
    {
        var userId = accountKeyMaterial.UserId;
        var keyName = userId.ToString();

        try
        {
            byte[]? protectedBytes = repository.Get(userId);

            if (protectedBytes is null)
            {
                return new AccountKeyUnlockResult.Failure(
                    "Windows Hello unlock is not enabled for this account. Unlock with your master password and enable Windows Hello again.");
            }

            byte[] decryptedBytes = WindowsHelloTpmKeyProtector.UnwrapUserKey(
                keyName,
                protectedBytes,
                hwnd);

            return new AccountKeyUnlockResult.Success(
                new UnlockedUserKey(userId, decryptedBytes));
        }
        catch (WindowsHelloAuthenticationCanceledException)
        {
            return new AccountKeyUnlockResult.Cancelled();
        }
        catch (WindowsHelloKeyUnavailableException)
        {
            RemoveWindowsHelloUnlock(userId);

            return new AccountKeyUnlockResult.Failure(
                "Windows Hello unlock is not enabled for this account. Unlock with your master password and enable Windows Hello again.");
        }
        catch (CryptographicException exception)
        {
            RemoveWindowsHelloUnlock(userId);
            return new AccountKeyUnlockResult.Failure(exception.Message);
        }
    }

    private void RemoveWindowsHelloUnlock(UserId userId)
    {
        WindowsHelloTpmKeyProtector.TryDeleteWrappingKey(userId.ToString());

        unitOfWork.Begin();
        repository.Remove(userId);
        unitOfWork.Commit();
    }
}
