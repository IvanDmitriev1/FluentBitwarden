using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data.Abstractions;

internal interface ISqliteConnectionFactory
{
    Task ExecuteAsync(Action<SqliteConnection> operation, CancellationToken cancellationToken = default);
    Task<T> ExecuteAsync<T>(Func<SqliteConnection, T> operation, CancellationToken cancellationToken = default);
}
