using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Session.Abstractions;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

internal sealed class RemoteAccountSessionManagerClient(IIpcClient ipcClient) : IAccountSessionManagerClient
{
    public ValueTask<bool> HasActiveSession() => ipcClient.SendAsync<bool>(IpcMessageTypes.Account.HasActiveSession);

    public ValueTask<AccountLoginOutcome>
        LogInAsync(AccountLoginRequest request, CancellationToken cancellationToken) =>
        ipcClient.SendAsync<AccountLoginRequest, AccountLoginOutcome>(request, cancellationToken);

    public ValueTask<GetAccountsResponse> GetAccounts() =>
        ipcClient.SendAsync<GetAccountsResponse>(IpcMessageTypes.Account.GetAccounts);

    public ValueTask<AccountUnlockOutcome> Unlock(AccountUnlockRequest request) =>
        ipcClient.SendAsync<AccountUnlockRequest, AccountUnlockOutcome>(request);
}