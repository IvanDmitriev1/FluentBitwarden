namespace FluentBitwarden.Platform.Hosting;

public interface IUiHostedServiceStarter
{
    bool IsStarted { get; }

    Task EnsureStartedAsync();
}
