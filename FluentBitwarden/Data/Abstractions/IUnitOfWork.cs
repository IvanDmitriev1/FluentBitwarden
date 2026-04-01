using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Abstractions;

public interface IUnitOfWork : IDisposable
{
    public SqliteTransaction Transaction { get; }

    void SaveChanges();
}