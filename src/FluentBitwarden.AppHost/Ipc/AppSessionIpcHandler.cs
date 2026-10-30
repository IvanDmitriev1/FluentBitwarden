using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Lock;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class AppSessionIpcHandler(
    IAppSessionAccessor appSessionAccessor,
    IAppSessionOperations sessionOperations) : IAppSessionClient, IIpcRequestsHandler
{
    public Task<AppSessionStateSnapshot> GetStateAsync(
        GetAppSessionStateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(appSessionAccessor.AppSessionState.ToSnapshot);

    public Task<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request, CancellationToken cancellationToken = default) =>
        sessionOperations.UnlockAsync(request, cancellationToken);

    public Task LockAsync(
        LockAppSessionRequest request, CancellationToken cancellationToken = default) =>
        sessionOperations.LockAsync(cancellationToken);
}
