using System.Diagnostics.CodeAnalysis;
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

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "commandText is only ever called with fixed pragma string literals from ApplyPragmas; no external input reaches it.")]
    private static void ExecutePragma(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
