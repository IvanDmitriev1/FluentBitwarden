using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    public SqliteConnectionFactory(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
        };

        _connectionString = builder.ToString();
    }

    private readonly string _connectionString;

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
