using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

public interface ISqliteConnectionFactory
{
    SqliteConnection OpenConnection();
}
