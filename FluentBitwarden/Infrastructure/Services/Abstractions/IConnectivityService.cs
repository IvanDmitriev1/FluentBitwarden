namespace FluentBitwarden.Infrastructure.Services.Abstractions;

public interface IConnectivityService
{
    bool HasInternetAccess { get; }

    event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
}

public sealed class ConnectivityChangedEventArgs(bool hasInternetAccess) : EventArgs
{
    public bool HasInternetAccess { get; } = hasInternetAccess;
}
