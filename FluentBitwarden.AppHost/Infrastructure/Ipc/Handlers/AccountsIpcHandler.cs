using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Accounts;
using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

[Fody.ConfigureAwait(false)]
internal sealed class AccountsIpcHandler(
    IAccountStore accountStore,
    IVaultSessionCoordinator vaultSessionCoordinator,
    IAccountLoginService accountLoginService) : IAccountsClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Account.GetUnlocked)]
    public ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default)
    {
        vaultSessionCoordinator.TryGetUnlockedSession(out var session);
        return ValueTask.FromResult(session?.Account);
    }

    [IpcMessageHandler(IpcMessageTypes.Account.GetUnlockedProfileDetails)]
    public ValueTask<AccountProfileDetails?> GetUnlockedAccountProfileDetails(CancellationToken cancellationToken = default)
    {
        if (!vaultSessionCoordinator.TryGetUnlockedSession(out var session))
            return ValueTask.FromResult<AccountProfileDetails?>(null);

        var details = accountStore.GetAccountProfileDetails(session.Account.UserId) ??
                      throw new InvalidOperationException("Unlocked account profile details are missing.");

        return ValueTask.FromResult<AccountProfileDetails?>(details);
    }

    [IpcMessageHandler(IpcMessageTypes.Account.GetAccounts)]
    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = accountStore.GetAccounts();
        return ValueTask.FromResult(accounts);
    }

    public ValueTask<AccountLoginOutcome> LoginAsync(
        AccountLoginRequest request,
        CancellationToken cancellationToken = default) =>
        accountLoginService.LoginAsync(request, cancellationToken);

    public ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken = default) =>
        vaultSessionCoordinator.UnlockAsync(request, cancellationToken);
}
