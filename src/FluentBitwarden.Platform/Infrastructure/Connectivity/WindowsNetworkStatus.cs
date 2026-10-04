using Windows.Networking.Connectivity;

namespace FluentBitwarden.Platform.Infrastructure.Connectivity;

internal sealed class WindowsNetworkStatus : INetworkStatus
{
    public bool HasInternetAccess =>
        NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel()
        == NetworkConnectivityLevel.InternetAccess;
}
