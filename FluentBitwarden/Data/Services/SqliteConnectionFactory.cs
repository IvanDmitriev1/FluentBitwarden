using FluentBitwarden.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Services;

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

    public Task ExecuteAsync(Action<SqliteConnection> operation, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var connection = CreateOpenConnection();
            operation(connection);
        }, cancellationToken);
    }

    public Task<T> ExecuteAsync<T>(Func<SqliteConnection, T> operation, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var connection = CreateOpenConnection();
            return operation(connection);
        }, cancellationToken);
    }

    private SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
