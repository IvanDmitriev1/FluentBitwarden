namespace FluentBitwarden.Infrastructure.Hosting;

public interface IUiHostedServiceStarter
{
    Task EnsureStartedAsync();
}
