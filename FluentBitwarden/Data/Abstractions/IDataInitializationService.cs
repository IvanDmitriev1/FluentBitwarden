namespace FluentBitwarden.Data.Abstractions;

public interface IDataInitializationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
