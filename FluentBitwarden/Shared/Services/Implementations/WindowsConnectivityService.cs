using Windows.Networking.Connectivity;
using FluentBitwarden.Shared.Services.Abstractions;

namespace FluentBitwarden.Shared.Services.Implementations;

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
