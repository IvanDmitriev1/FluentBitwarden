using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;
using FluentBitwarden.AppHost.Modules.Account.Persistance;
using FluentBitwarden.Contracts.Modules.Accounts.Login;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountService(
    IAccountWindowsHelloService accountWindowsHelloService,
    AccountKeyMaterialRepository keyMaterialRepository,
    AccountProfileRepository accountProfileRepository) : IAccountService
{
    public AccountProfile[] GetAccounts() => accountProfileRepository.GetAccounts();

    public AccountProfile? GetAccount(UserId userId) => accountProfileRepository.GetById(userId);

    public Task LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method)
    {
        if (keyMaterialRepository.GetById(userId) is not { } accountKeyMaterial)
            return new AccountKeyUnlockResult.RequiresOnlineReauthentication();

        return method switch
        {
            AccountUnlockMethod.MasterPassword masterPassword =>
                accountKeyMaterial.MasterPasswordUnlock(masterPassword.Password),
            AccountUnlockMethod.WindowsHello windowsHello =>
                accountWindowsHelloService.Unlock(accountKeyMaterial, (IntPtr)windowsHello.OwnerWindow.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }
}
