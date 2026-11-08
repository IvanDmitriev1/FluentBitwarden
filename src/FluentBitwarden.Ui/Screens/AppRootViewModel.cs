using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Unlock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Screens;

public sealed partial class AppRootViewModel(
    IServiceProvider serviceProvider,
    IMessenger messenger,
    IAppSessionClient appSessionClient,
    IIpcExceptionNotifier exceptionNotifier,
    IIpcEventClient eventClient) : ObservableRecipient(messenger), IHostedService, IRecipient<AppRootNavigationRequest>
{
    private IDisposable? _sessionStateChangedSubscription;

    [ObservableProperty]
    public partial object Content { get; private set; } = new LoadingViewModel();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionStateChangedSubscription = eventClient.Subscribe<AppSessionStateChangedEvent>((e, _) =>
            App.Current.DispatcherQueue.EnqueueAsync(() => UpdateContent(e.StateSnapshot)));

        exceptionNotifier.UnlockedSessionRequired += OnUnlockedSessionRequired;

        await App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            IsActive = true;
            await RefreshSessionAsync();
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionStateChangedSubscription?.Dispose();
        _sessionStateChangedSubscription = null;
        exceptionNotifier.UnlockedSessionRequired -= OnUnlockedSessionRequired;

        IsActive = false;
        return Task.CompletedTask;
    }

    void IRecipient<AppRootNavigationRequest>.Receive(AppRootNavigationRequest message)
    {
        UpdateContent(message.State);
    }

    private void OnUnlockedSessionRequired(UnlockedSessionRequiredException exception) =>
        App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync).SafeFireAndForget();

    private async Task RefreshSessionAsync()
    {
        var session = await appSessionClient.GetStateAsync(new GetAppSessionStateRequest(), CancellationToken.None);
        UpdateContent(session);
    }

    private void UpdateContent(AppSessionStateSnapshot session)
    {
        Content = session switch
        {
            AppSessionStateSnapshot.NotAuthenticated => new SetupViewModel(),
            AppSessionStateSnapshot.Locked locked => Content =
                ActivatorUtilities.CreateInstance<UnlockViewModel>(serviceProvider, locked.Account),
            AppSessionStateSnapshot.Unlocked => new ShellViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(session))
        };
    }
}
