namespace FluentBitwarden.Abstractions.Storage;

internal interface IDbInitializerService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
