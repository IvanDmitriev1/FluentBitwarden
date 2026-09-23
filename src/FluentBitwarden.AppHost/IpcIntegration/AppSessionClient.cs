using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.AppSession.Lock;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.IpcIntegration;

internal sealed class AppSessionClient : IAppSessionClient, IIpcRequestsHandler
{
    public Task<AppSessionSnapshot> GetSnapshotAsync(
        GetAppSessionSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task LockAsync(
        LockAppSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
