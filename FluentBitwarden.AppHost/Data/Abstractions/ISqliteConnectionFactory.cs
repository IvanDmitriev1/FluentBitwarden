using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Data.Abstractions;

public interface ISqliteConnectionFactory
{
    string ConnectionString { get; }
    SqliteConnection OpenConnection();
}
