using FluentBitwarden.Modules.Connectivity.Abstractions;
using Windows.Networking.Connectivity;

namespace FluentBitwarden.Modules.Connectivity.Services;

internal sealed class WindowsConnectivityService : IConnectivityService, IDisposable
{
    public WindowsConnectivityService()
    {
        NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
    }

    public bool HasInternetAccess => GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;

    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

    public void Dispose()
    {
        NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
    }

    private static NetworkConnectivityLevel GetNetworkConnectivityLevel()
        => NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel()
           ?? NetworkConnectivityLevel.None;

    private void OnNetworkStatusChanged(object sender)
    {
        ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(HasInternetAccess));
    }
}
