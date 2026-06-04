namespace FluentBitwarden.Platform.Hosting;

public interface IUiHostedServiceStarter
{
    Task EnsureStartedAsync();
}
