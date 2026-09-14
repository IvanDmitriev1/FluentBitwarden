using AsyncAwaitBestPractices;
using CommunityToolkit.WinUI;
using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.UiCommand;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Windows.Networking.Connectivity;

namespace FluentBitwarden.Application.Implementations;

internal sealed class AppCoordinator : IAppCoordinator, IDisposable
{
    private readonly record struct FlowVersion(uint Id, AppCoordinator Coordinator)
    {
        public bool IsCurrent => Volatile.Read(ref Coordinator._flowId) == Id;
    };

    public AppCoordinator(
        IAppSessionResolver sessionResolver,
        IWindowManager windowManager,
        IIpcEventClient eventClient,
        IUiHostedServiceManager hostedServiceManager)
    {
        _sessionResolver = sessionResolver;
        _windowManager = windowManager;
        _hostedServiceManager = hostedServiceManager;

        eventClient.Subscribe<VaultSessionStatusChangedEvent>((_, _) =>
            App.Current.DispatcherQueue.EnqueueAsync(RefreshSessionAsync));

        //NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
    }

    private readonly IAppSessionResolver _sessionResolver;
    private readonly IWindowManager _windowManager;
    private readonly IUiHostedServiceManager _hostedServiceManager;

    private readonly SemaphoreSlim _flowGate = new(1, 1);
    private uint _flowId;

    private bool _firstStart;
    private OpenVaultCipherIntent? _currentIntent;

    public event Action<AppSessionState, UnlockPageParameter?, OpenVaultCipherIntent?>? SessionStateApplied;

    private FlowVersion BeginNewFlow => new(Interlocked.Increment(ref _flowId), this);

    public AppSessionState SessionState
    {
        get;
        set
        {
            /*Debug.Assert(
                SessionState == AppSessionState.Unknown || value == AppSessionState.Unknown,
                $"Application session transition '{SessionState}' to '{value}' must pass through Unknown.");
                */

            field = value;
        }
    } = AppSessionState.Unknown;

    public async Task HandleActivation(UiCliCommand command)
    {
        switch (command)
        {
            case UiCliCommand.ExitCommand:
                App.Current.Exit();
                return;
            case UiCliCommand.OverlayCommand:
                _windowManager.ShowOrCreateWindow(WindowMode.Overlay);
                break;
            default:
                _windowManager.ShowOrCreateWindow(WindowMode.Main);
                break;
        }

        _currentIntent = command switch
        {
            UiCliCommand.OpenItemCommand openItemCommand =>
                new OpenVaultCipherIntent(openItemCommand.Id),
            _ => _currentIntent
        };

        if (!_firstStart)
        {
            await _hostedServiceManager.EnsureProcessServicesStarted();
            _firstStart = true;
        }

        await RefreshSessionAsync();
        _currentIntent = null;
    }

    public async Task RefreshSessionAsync()
    {
        var version = BeginNewFlow;
        await _flowGate.WaitAsync();

        try
        {
            if (!version.IsCurrent)
            {
                return;
            }

            SessionState = AppSessionState.Unknown;
            var resolution = await _sessionResolver.ResolveAsync();

            ApplySessionResolution(resolution);
        }
        finally
        {
            _flowGate.Release();
        }
    }

    public Task BeginSignIn()
    {
        ApplySessionResolution(new AppSessionResolution.LoggedOutResolution());
        return Task.CompletedTask;
    }

    private void ApplySessionResolution(AppSessionResolution resolution)
    {
        UnlockPageParameter? unlockParameter = null;

        switch (resolution)
        {
            case AppSessionResolution.LoggedOutResolution:
                SessionState = AppSessionState.LoggedOut;
                break;
            case AppSessionResolution.LockedResolution:
                SessionState = AppSessionState.Locked;
                var lockedResolution = (AppSessionResolution.LockedResolution)resolution;
                unlockParameter = new UnlockPageParameter(
                    lockedResolution.Accounts,
                    lockedResolution.SelectedAccount);
                break;
            case AppSessionResolution.UnlockedResolution:
                SessionState = AppSessionState.Unlocked;
                break;
        }

        SessionStateApplied?.Invoke(SessionState, unlockParameter, _currentIntent);
    }

    private void OnNetworkStatusChanged(object? sender)
    {
        if (!NetworkInformation.HasInternetAccess)
        {
            return;
        }

        if (SessionState != AppSessionState.LoggedOut)
        {
            return;
        }

        RefreshSessionAsync().SafeFireAndForget();
    }

    public void Dispose() => _flowGate.Dispose();
}
