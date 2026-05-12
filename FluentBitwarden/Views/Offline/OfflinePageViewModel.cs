using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Offline.Models;

namespace FluentBitwarden.Views.Offline;

public sealed partial class OfflinePageViewModel(
    INavigationService navigationService,
    IConnectivityService connectivityService) : ObservableObject, IPageLifecycleAware<OfflinePageParameter>
{
    private bool _isActive;

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    public Task OnLoadingAsync(OfflinePageParameter param, CancellationToken cancellationToken)
    {
        ApplyReasonText(param.Reason);

        _isActive = true;

        connectivityService.ConnectivityChanged += OnConnectivityChanged;

        if (connectivityService.HasInternetAccess)
            navigationService.NavigateTo<LoadingPage>();

        return Task.CompletedTask;
    }

    public void OnUnloading()
    {
        _isActive = false;
        connectivityService.ConnectivityChanged -= OnConnectivityChanged;
    }

    [RelayCommand]
    private void Retry()
    {
        if (!connectivityService.HasInternetAccess)
            return;

        navigationService.NavigateTo<LoadingPage>();
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

        if (App.Current.DispatcherQueue.HasThreadAccess)
        {
            navigationService.NavigateTo<LoadingPage>();
            return;
        }

        _ = App.Current.DispatcherQueue.TryEnqueue(() => navigationService.NavigateTo<LoadingPage>());
    }
}
