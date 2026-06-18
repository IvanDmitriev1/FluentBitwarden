using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts;

[Fody.ConfigureAwait(false)]
internal sealed class AccountsClientHandler(
    IStoredAccountStore accountStore,
    IAccountUnlockService accountUnlockService,
    IAccountLoginService accountLoginService,
    IUnlockedAccountAccessor unlockedAccountAccessor,
    IVaultWorkspace vaultWorkspace) : IAccountsClient, IIpcRequestsHandler
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

    public async ValueTask<AccountUnlockOutcome> UnlockAsync(AccountUnlockRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = accountUnlockService.Unlock(request);
        if (result is not AccountUnlockOutcome.Success)
            return result;

        await vaultWorkspace.OpenAsync(unlockedAccountAccessor.UserKey, cancellationToken);
        return result;
    }
}
