using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Shared.Services.Abstractions;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell.Navigation;
using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Views.Offline;

public sealed partial class OfflinePageViewModel(
    INavigationService navigationService,
    IConnectivityService connectivityService) : ObservableObject, IPageLifecycleAware<OfflinePageParameter>
{
    private bool _isActive;
    private bool _navigationRequested;
    private DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    public Task OnLoadingAsync(OfflinePageParameter param, CancellationToken cancellationToken)
    {
        ApplyReasonText(param.Reason);

        _isActive = true;
        _navigationRequested = false;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        connectivityService.ConnectivityChanged += OnConnectivityChanged;

        if (connectivityService.HasInternetAccess)
            NavigateToSetupWhenOnline();

        return Task.CompletedTask;
    }

    public void OnUnloading()
    {
        _isActive = false;
        _navigationRequested = false;
        _dispatcherQueue = null;
        connectivityService.ConnectivityChanged -= OnConnectivityChanged;
    }

    [RelayCommand]
    private void Retry()
    {
        if (!connectivityService.HasInternetAccess)
            return;

        NavigateToSetupWhenOnline();
    }

    private void ApplyReasonText(OfflinePageReason reason)
    {
        switch (reason)
        {
            case OfflinePageReason.FirstSignInRequiresInternet:
                Title = "Internet required";
                Message = "Sign in requires internet access. Reconnect and try again.";
                return;

            case OfflinePageReason.ReauthRequiresInternet:
                Title = "Reauthentication required";
                Message = "This account needs online reauthentication before it can be unlocked.";
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (!e.HasInternetAccess || !_isActive)
            return;

        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
        {
            NavigateToSetupWhenOnline();
            return;
        }

        _ = _dispatcherQueue.TryEnqueue(NavigateToSetupWhenOnline);
    }

    private void NavigateToSetupWhenOnline()
    {
        if (!_isActive || _navigationRequested || !connectivityService.HasInternetAccess)
            return;

        _navigationRequested = true;
        navigationService.NavigateTo<SetupPage>();
    }
}
