using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Abstractions;

internal interface IConnectionFactory
{
    Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
