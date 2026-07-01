using FluentBitwarden.AppHost.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Data.Implementations;

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

        ConnectionString = builder.ToString();
    }

    public string ConnectionString { get; }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        ApplyPragmas(connection);
        return connection;
    }

    private static void ApplyPragmas(SqliteConnection connection)
    {
        ExecutePragma(connection, "PRAGMA foreign_keys = ON;");
        ExecutePragma(connection, "PRAGMA journal_mode = WAL;");
        ExecutePragma(connection, "PRAGMA synchronous = NORMAL;");
        ExecutePragma(connection, "PRAGMA busy_timeout = 1000;");
    }

    private static void ExecutePragma(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
