namespace FluentBitwarden.Data.Abstractions;

public interface IDataInitializationService
{
    void Initialize(CancellationToken cancellationToken = default);
}
