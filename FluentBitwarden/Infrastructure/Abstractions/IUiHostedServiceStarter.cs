namespace FluentBitwarden.Infrastructure.Abstractions;

public interface IUiHostedServiceStarter
{
    bool IsStarted { get; }

    Task EnsureStartedAsync();
}
