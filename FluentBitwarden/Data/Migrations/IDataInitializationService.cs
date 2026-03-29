namespace FluentBitwarden.Data.Migrations;

public interface IDataInitializationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
