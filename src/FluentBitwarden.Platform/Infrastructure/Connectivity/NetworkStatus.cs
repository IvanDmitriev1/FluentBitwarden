namespace FluentBitwarden.Platform.Infrastructure.Connectivity;

public interface INetworkStatus
{
    bool HasInternetAccess { get; }
}
