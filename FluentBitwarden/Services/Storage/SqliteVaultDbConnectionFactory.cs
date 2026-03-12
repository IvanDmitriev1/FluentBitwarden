using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultDbConnectionFactory(IAppPaths paths) : IVaultDbConnectionFactory
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = paths.VaultDbFilePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
    }.ToString();

    public ValueTask<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        => OpenConnectionCoreAsync(cancellationToken);

    private async ValueTask<SqliteConnection> OpenConnectionCoreAsync(CancellationToken cancellationToken)
    {
        Batteries_V2.Init();

        SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
