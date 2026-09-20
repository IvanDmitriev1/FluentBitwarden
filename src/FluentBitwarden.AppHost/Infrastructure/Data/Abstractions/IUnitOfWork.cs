namespace FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

internal interface IUnitOfWork : IDbSession
{
    void Begin();
    void Commit();
}
