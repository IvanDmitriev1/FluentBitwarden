using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Infrastructure.Clients;

internal sealed class RemoteAccountsClient(IIpcClient ipcClient) : IAccountsClient
{
    public ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile?>(IpcMessageTypes.Account.GetUnlocked, cancellationToken);

    public ValueTask<AccountProfileDetails?> GetUnlockedAccountProfileDetails(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfileDetails?>(IpcMessageTypes.Account.GetUnlockedProfileDetails, cancellationToken);

    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile[]>(IpcMessageTypes.Account.GetAccounts, cancellationToken);

    public ValueTask<AccountLoginOutcome> LoginAsync(AccountLoginRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountLoginRequest, AccountLoginOutcome>(request, cancellationToken);

    public ValueTask<AccountUnlockOutcome> UnlockAsync(AccountUnlockRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountUnlockRequest, AccountUnlockOutcome>(request, cancellationToken);

    public async ValueTask LogOutAsync(
        UserId accountId,
        CancellationToken cancellationToken = default) =>
        _ = await ipcClient.SendAsync<AccountLogOutRequest, IpcVoid>(
            new AccountLogOutRequest(accountId),
            cancellationToken);
}
