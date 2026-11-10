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
    IIpcEventClient eventClient) : ObservableRecipient(messenger), IHostedService,
    IRecipient<AppRootNavigationRequest>, IRecipient<AppRootSessionRefreshRequest>
{
    private AppSessionStateSnapshot? _appliedSession;

    [ObservableProperty]
    public partial object Content { get; private set; } = new LoadingViewModel();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = eventClient.Subscribe<AppSessionStateChangedEvent>((e, _) =>
            App.Current.DispatcherQueue.EnqueueAsync(() => UpdateContent(e.StateSnapshot)));

        exceptionNotifier.UnlockedSessionRequired += OnUnlockedSessionRequired;

        await App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            IsActive = true;
            await RefreshSessionAsync();
        });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    void IRecipient<AppRootNavigationRequest>.Receive(AppRootNavigationRequest message) =>
        UpdateContent(message.State);

    void IRecipient<AppRootSessionRefreshRequest>.Receive(AppRootSessionRefreshRequest message) =>
        App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync).SafeFireAndForget();

    private void OnUnlockedSessionRequired(UnlockedSessionRequiredException exception) =>
        App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync).SafeFireAndForget();

    private async Task RefreshSessionAsync()
    {
        var session = await appSessionClient.GetStateAsync(new GetAppSessionStateRequest(), CancellationToken.None);
        if (IsActive)
            UpdateContent(session);
    }

    private void UpdateContent(AppSessionStateSnapshot session)
    {
        if (!IsActive || Equals(_appliedSession, session))
            return;

        _appliedSession = session;
        Content = session switch
        {
            AppSessionStateSnapshot.NotAuthenticated => serviceProvider.GetRequiredService<SetupViewModel>(),
            AppSessionStateSnapshot.Locked locked =>
                ActivatorUtilities.CreateInstance<UnlockViewModel>(serviceProvider, locked.Account),
            AppSessionStateSnapshot.Unlocked unlocked =>
                ActivatorUtilities.CreateInstance<ShellViewModel>(serviceProvider, unlocked.Account),
            _ => throw new ArgumentOutOfRangeException(nameof(session))
        };
    }
}
