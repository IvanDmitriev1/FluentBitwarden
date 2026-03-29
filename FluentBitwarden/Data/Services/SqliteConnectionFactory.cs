using FluentBitwarden.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Services;

internal sealed class SqliteConnectionFactory : IConnectionFactory
{
    public SqliteConnectionFactory(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        };

        _connectionString = builder.ToString();
    }

    private readonly string _connectionString;

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
