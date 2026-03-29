using Dapper;
using FluentBitwarden.Data.Abstractions;

namespace FluentBitwarden.Data.Migrations;

internal sealed class DataInitializationService(IConnectionFactory connectionFactory) : IDataInitializationService
{
    private const string CreateAccountsTableSql =
        """
        CREATE TABLE IF NOT EXISTS accounts (
            user_id TEXT PRIMARY KEY NOT NULL,
            email TEXT NOT NULL,
            api_base TEXT NOT NULL,
            identity_base TEXT NOT NULL,
            notifications_base TEXT NOT NULL,
            encrypted_user_key TEXT NOT NULL,
            encrypted_private_key TEXT NOT NULL,
            kdf_type INTEGER NOT NULL,
            kdf_iterations INTEGER NOT NULL,
            kdf_memory_mib INTEGER NULL,
            kdf_parallelism INTEGER NULL,
            last_sync_at_unix_ms INTEGER NOT NULL
        );
        """;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        CommandDefinition command = new(
            CreateAccountsTableSql,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
