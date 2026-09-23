using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.IpcIntegration;

internal sealed class AppSessionClient : IAppSessionClient, IIpcRequestsHandler
{
    public ValueTask<AppSessionSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
