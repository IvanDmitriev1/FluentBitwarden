using Windows.Networking.Connectivity;

namespace FluentBitwarden.Contracts.Extensions;

public static class NetworkInformationExtensions
{
    extension(NetworkInformation)
    {
        public static bool HasInternetAccess => NetworkInformation.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;

        private static NetworkConnectivityLevel GetNetworkConnectivityLevel()
            => NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel()
               ?? NetworkConnectivityLevel.None;
    }
}