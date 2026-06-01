namespace FluentBitwarden.Platform.AppHost.Lifetime;

public interface IAppHostLifetimeService
{
    Task ShutdownAppHostAsync(CancellationToken cancellationToken = default);
}
