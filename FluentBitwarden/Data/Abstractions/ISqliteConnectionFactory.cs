using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Abstractions;

public interface ISqliteConnectionFactory
{
    SqliteConnection OpenConnection();
}
