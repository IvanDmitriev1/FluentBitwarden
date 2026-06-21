namespace FluentBitwarden.Infrastructure.AppLifecycle;

public interface IUiHostedServiceStarter
{
    Task EnsureStartedAsync();
}
