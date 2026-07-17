using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Sessions;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.AppHost.Modules.Sessions.Ipc;

internal sealed class SessionIpcHandler(IVaultSessionManager sessionManager)
    : ISessionClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Session.GetUnlockedAccount)]
    public ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default)
    {
        sessionManager.TryGetUnlockedSession(out var session);
        return ValueTask.FromResult(session?.Account);
    }

    [IpcMessageHandler(IpcMessageTypes.Session.GetStatus)]
    public ValueTask<VaultSessionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = sessionManager.TryGetUnlockedSession(out _)
            ? VaultSessionStatus.Unlocked
            : VaultSessionStatus.Locked;
        return ValueTask.FromResult(status);
    }

    public ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken = default) =>
        new(sessionManager.UnlockAsync(request, cancellationToken));

    [IpcMessageHandler(IpcMessageTypes.Session.Lock)]
    public async ValueTask LockAsync(CancellationToken cancellationToken = default) =>
        await sessionManager.LockAsync(cancellationToken);
}
