using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Data.Abstractions;

public interface IUnitOfWork : IDisposable
{
    public SqliteTransaction Transaction { get; }

    void SaveChanges();
}