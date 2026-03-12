using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultDbConnectionFactory
{
    ValueTask<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
