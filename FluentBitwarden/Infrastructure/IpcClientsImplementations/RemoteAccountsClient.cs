using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Accounts;
using FluentBitwarden.Contracts.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

internal sealed class RemoteAccountsClient(IIpcClient ipcClient) : IAccountsClient
{
    public ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile?>(IpcMessageTypes.Account.GetUnlocked, cancellationToken);

    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile[]>(IpcMessageTypes.Account.GetAccounts, cancellationToken);

    public ValueTask<AccountLoginOutcome> LoginAsync(AccountLoginRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountLoginRequest, AccountLoginOutcome>(request, cancellationToken);

    public ValueTask<AccountUnlockOutcome> UnlockAsync(AccountUnlockRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountUnlockRequest, AccountUnlockOutcome>(request, cancellationToken);
}