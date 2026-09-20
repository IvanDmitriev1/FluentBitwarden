using Dapper;
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
            DefaultTimeout = 5
        };

        _connectionStr = builder.ToString();
    }

    private readonly string _connectionStr;

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionStr);
        connection.Open();

        connection.Execute("PRAGMA synchronous = FULL;");

        return connection;
    }
}
