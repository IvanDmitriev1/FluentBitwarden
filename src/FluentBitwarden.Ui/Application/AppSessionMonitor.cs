using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Ui.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Application;

internal sealed class AppSessionMonitor(
    IAppSessionClient appSessionClient,
    IIpcExceptionNotifier exceptionNotifier,
    IIpcEventClient eventClient,
    IAppRouter windowRouter) : IHostedService, IAppSession
{
    private long _sessionRevision;

    public AppSessionStateSnapshot? CurrentSession { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        eventClient.Subscribe<AppSessionStateChangedEvent>(HandleSessionEventAsync);
        exceptionNotifier.UnlockedSessionRequired += OnUnlockedSessionRequired;
        await RefreshSessionAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task RefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        long revision = Interlocked.Increment(ref _sessionRevision);

        try
        {
            AppSessionStateSnapshot session = await appSessionClient.GetStateAsync(
                new GetAppSessionStateRequest(), cancellationToken);

            if (revision != Volatile.Read(ref _sessionRevision) || cancellationToken.IsCancellationRequested)
                return;

            await App.Current.DispatcherQueue.EnqueueAsync(() =>
            {
                if (revision == Volatile.Read(ref _sessionRevision))
                    ApplySession(session);
            });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task HandleSessionEventAsync(AppSessionStateChangedEvent sessionEvent, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _sessionRevision);
        return App.Current.DispatcherQueue.EnqueueAsync(() => ApplySession(sessionEvent.StateSnapshot));
    }

    private void ApplySession(AppSessionStateSnapshot session)
    {
        if (CurrentSession == session)
            return;

        CurrentSession = session;
        windowRouter.Navigate(session);
    }

    private void OnUnlockedSessionRequired(UnlockedSessionRequiredException exception) =>
        RefreshSessionAsync().SafeFireAndForget();
}
