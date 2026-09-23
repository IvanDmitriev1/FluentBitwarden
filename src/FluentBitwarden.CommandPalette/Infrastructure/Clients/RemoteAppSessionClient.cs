using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.AppSession.Lock;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteAppSessionClient(IIpcClient ipcClient) : IAppSessionClient
{
    public Task<AppSessionSnapshot> GetSnapshotAsync(
        GetAppSessionSnapshotRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetAppSessionSnapshotRequest, AppSessionSnapshot>(request, cancellationToken);

    public Task<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SessionUnlockRequest, SessionUnlockOutcome>(request, cancellationToken);

    public async Task LockAsync(
        LockAppSessionRequest request,
        CancellationToken cancellationToken = default) =>
        await ipcClient.SendAsync<LockAppSessionRequest, IpcVoid>(request, cancellationToken);
}
