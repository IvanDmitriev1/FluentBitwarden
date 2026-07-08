using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed class AccountUnlockService(
    IAccountStore accountStore,
    MasterPasswordUnlocker masterPasswordUnlocker,
    WindowsHelloUnlocker windowsHelloUnlocker) : IAccountUnlockService
{
    public AccountUnlockResult Unlock(AccountUnlockRequest request)
    {
        var keyMaterial = accountStore.GetKeyMaterial(request.Account.UserId);
        if (keyMaterial is null)
            return AccountUnlockResult.WithoutUserKey(new AccountUnlockOutcome.RequiresOnlineReauth());

        return request switch
        {
            AccountUnlockRequest.MasterPasswordRequest password =>
                masterPasswordUnlocker.Unlock(keyMaterial, password.MasterPassword),

            AccountUnlockRequest.WindowsHelloRequest windowsHello =>
                windowsHelloUnlocker.Unlock(keyMaterial, windowsHello.OwnerWindowHandle),

            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
    }
}
