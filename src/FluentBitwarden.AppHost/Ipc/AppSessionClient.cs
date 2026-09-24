using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Lock;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class AppSessionClient(IAppSessionService sessionService) : IAppSessionClient, IIpcRequestsHandler
{
    public Task<AppSessionState> GetStateAsync(
        GetAppSessionStateRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(sessionService.State);

    public Task<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(sessionService.Unlock(request));

    public Task LockAsync(
        LockAppSessionRequest request, CancellationToken cancellationToken = default) =>
        sessionService.LockAsync(cancellationToken);
}
