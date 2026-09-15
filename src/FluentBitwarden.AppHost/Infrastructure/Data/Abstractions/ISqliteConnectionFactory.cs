using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

public interface ISqliteConnectionFactory
{
    string ConnectionString { get; }
    SqliteConnection OpenConnection();
}
