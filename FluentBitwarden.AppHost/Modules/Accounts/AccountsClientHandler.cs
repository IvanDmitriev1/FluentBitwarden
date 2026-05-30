using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Accounts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Modules.Vault.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal sealed class AccountsClientHandler(
    IStoredAccountStore accountStore,
    IAccountUnlockService accountUnlockService,
    IAccountLoginService accountLoginService,
    IUnlockedAccountAccessor unlockedAccountAccessor,
    IVaultService vaultService) : IAccountsClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Account.GetUnlocked)]
    public ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(unlockedAccountAccessor.HasUnlockedAccount
            ? unlockedAccountAccessor.CurrentAccount
            : null);

    [IpcMessageHandler(IpcMessageTypes.Account.GetAccounts)]
    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = accountStore.GetAccounts();
        return ValueTask.FromResult(accounts);
    }

    public ValueTask<AccountLoginOutcome> LoginAsync(AccountLoginRequest request,
        CancellationToken cancellationToken = default) => accountLoginService.LoginAsync(request, cancellationToken);

    public ValueTask<AccountUnlockOutcome> UnlockAsync(AccountUnlockRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = accountUnlockService.Unlock(request);
        if (result is AccountUnlockOutcome.Success)
        {
            vaultService.LoadLocalVault();
        }

        return ValueTask.FromResult(result);
    }
}