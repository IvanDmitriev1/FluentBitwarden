using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountWindowsHelloService
{
    bool IsEnabled(UserId userId);
    void Enable(UnlockedUserKey userKey, IntPtr hwnd);
    void Disable(UserId userId);

    AccountKeyUnlockResult Unlock(AccountKeyMaterial accountKeyMaterial, IntPtr hwnd);
}
