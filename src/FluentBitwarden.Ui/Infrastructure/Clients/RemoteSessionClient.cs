using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Sessions;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Infrastructure.Clients;

[Fody.ConfigureAwait(false)]
internal sealed class RemoteSessionClient(IIpcClient ipcClient) : ISessionClient
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
