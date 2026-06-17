using Windows.Networking.Connectivity;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Views.Startup;

namespace FluentBitwarden.ViewModels.Shell.Offline;

public sealed partial class OfflinePageViewModel(
    INavigationService navigationService) : ObservableObject, IPageLifecycleAware<OfflinePageParameter>
{
    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    public Task OnLoadingAsync(OfflinePageParameter param, CancellationToken cancellationToken)
    {
        ApplyReasonText(param.Reason);

        NetworkInformation.NetworkStatusChanged += NetworkInformationOnNetworkStatusChanged;
        return Task.CompletedTask;
    }

    public void OnUnloading()
    {
        NetworkInformation.NetworkStatusChanged -= NetworkInformationOnNetworkStatusChanged;
    }

    [RelayCommand]
    private void Retry()
    {
        if (!NetworkInformation.HasInternetAccess)
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

    private void NetworkInformationOnNetworkStatusChanged(object sender)
    {
        if (!NetworkInformation.HasInternetAccess)
            return;

        if (App.Current.DispatcherQueue.HasThreadAccess)
        {
            navigationService.NavigateTo<LoadingPage>();
            return;
        }

        _ = App.Current.DispatcherQueue.TryEnqueue(() => navigationService.NavigateTo<LoadingPage>());
    }
}
