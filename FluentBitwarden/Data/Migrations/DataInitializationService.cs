using Dapper;
using FluentBitwarden.Data.Abstractions;

namespace FluentBitwarden.Data.Migrations;

internal sealed class DataInitializationService(ISqliteConnectionFactory connectionFactory) : IDataInitializationService
{
    private const string CreateAccountsTableSql =
        """
        CREATE TABLE IF NOT EXISTS accounts (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE,
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

    private const string CreateAccountSecurityTableSql =
        """
        CREATE TABLE IF NOT EXISTS account_security (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE,
            has_pin INTEGER NOT NULL,
            has_windows_hello INTEGER NOT NULL,
            FOREIGN KEY(user_id) REFERENCES accounts(user_id) ON DELETE CASCADE
        );
        """;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        connectionFactory.ExecuteAsync(connection =>
        {
            connection.Execute(new CommandDefinition(CreateAccountsTableSql, cancellationToken: cancellationToken));
            connection.Execute(new CommandDefinition(CreateAccountSecurityTableSql, cancellationToken: cancellationToken));
        }, cancellationToken);
}
