using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Abstractions;

internal interface ISqliteConnectionFactory
{
    SqliteConnection OpenConnection();
}
