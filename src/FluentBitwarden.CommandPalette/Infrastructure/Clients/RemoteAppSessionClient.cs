using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteAppSessionClient(IIpcClient ipcClient) : IAppSessionClient
{
    public ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile?>(IpcMessageTypes.Session.GetUnlockedAccount, cancellationToken);

    public ValueTask<VaultSessionStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<VaultSessionStatus>(IpcMessageTypes.Session.GetStatus, cancellationToken);

    public ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountUnlockRequest, AccountUnlockOutcome>(request, cancellationToken);

    public async ValueTask LockAsync(CancellationToken cancellationToken = default) =>
        await ipcClient.SendAsync<IpcVoid>(IpcMessageTypes.Session.Lock, cancellationToken);
}
