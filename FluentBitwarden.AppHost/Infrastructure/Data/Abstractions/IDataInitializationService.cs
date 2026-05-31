namespace FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

public interface IDataInitializationService
{
    void Initialize(CancellationToken cancellationToken = default);
}
