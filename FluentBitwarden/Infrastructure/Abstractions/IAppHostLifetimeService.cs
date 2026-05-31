namespace FluentBitwarden.Infrastructure.Abstractions;

public interface IAppHostLifetimeService
{
    Task ShutdownAppHostAsync(CancellationToken cancellationToken = default);
}
