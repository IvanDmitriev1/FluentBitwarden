using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Session.Abstractions;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

internal sealed class RemoteAccountSessionManagerClient(IIpcClient ipcClient) : IAccountSessionManagerClient
{
    public async ValueTask<bool> HasActiveSession()
    {
        var result = await ipcClient.SendAsync<bool>(IpcMessageTypes.Account.HasActiveSession);
        return result.GetValueOrThrow();
    }

    public async ValueTask<AccountLoginOutcome> SignInAsync(AccountLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await ipcClient.SendAsync<AccountLoginRequest, AccountLoginOutcome>(request, cancellationToken);
        return result.GetValueOrThrow();
    }

    public async ValueTask<IReadOnlyList<AccountProfile>> GetAccounts()
    {
        var result = await ipcClient.SendAsync<IReadOnlyList<AccountProfile>>(IpcMessageTypes.Account.GetAccounts);
        return result.GetValueOrThrow();
    }

    public async ValueTask<AccountUnlockOutcome> Unlock(AccountUnlockRequest request)
    {
        var result = await ipcClient.SendAsync<AccountUnlockRequest, AccountUnlockOutcome>(request);
        return result.GetValueOrThrow();
    }
}