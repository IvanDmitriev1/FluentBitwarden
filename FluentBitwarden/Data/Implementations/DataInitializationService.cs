using Dapper;
using FluentBitwarden.Data.Abstractions;
using System.Data;
using System.Linq;

namespace FluentBitwarden.Data.Implementations;

internal sealed class DataInitializationService(ISqliteConnectionFactory connectionFactory) : IDataInitializationService
{
    private const string CreateTableSql =
        """
        CREATE TABLE IF NOT EXISTS accounts (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE,
            email TEXT NOT NULL,
            api_base TEXT NOT NULL,
            identity_base TEXT NOT NULL,
            notifications_base TEXT NOT NULL,
            last_sync_at_unix_ms INTEGER NOT NULL
        );
        
        CREATE TABLE IF NOT EXISTS account_decryption (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES accounts(user_id) ON DELETE CASCADE,
            salt                 TEXT NOT NULL,
            encrypted_user_key   TEXT NOT NULL,
            encrypted_private_key TEXT NOT NULL,
            kdf_type             INTEGER NOT NULL,
            kdf_iterations       INTEGER NOT NULL,
            kdf_memory_mib       INTEGER NULL,
            kdf_parallelism      INTEGER NULL
        );
        
        CREATE TABLE IF NOT EXISTS folders (
            row_id             INTEGER PRIMARY KEY,
            user_id   TEXT NOT NULL COLLATE NOCASE,
            folder_id TEXT NOT NULL COLLATE NOCASE,
            revision_date_unix_ms INTEGER NOT NULL,
            encrypted_name    TEXT NOT NULL,
        
            UNIQUE (user_id, folder_id),
        
            FOREIGN KEY (user_id) REFERENCES accounts(user_id) ON DELETE CASCADE
        );
        
        CREATE TABLE IF NOT EXISTS collections (
            row_id         INTEGER PRIMARY KEY,
            user_id        TEXT NOT NULL COLLATE NOCASE,
            collection_id  TEXT NOT NULL COLLATE NOCASE,
            organization_id TEXT NULL,
            read_only      INTEGER NOT NULL,
            manage         INTEGER NOT NULL,
            hide_passwords INTEGER NOT NULL,
            collection_type INTEGER NULL,
            encrypted_name TEXT NOT NULL,
        
            UNIQUE (user_id, collection_id),
        
            FOREIGN KEY (user_id) REFERENCES accounts(user_id) ON DELETE CASCADE
        );
        
        CREATE TABLE IF NOT EXISTS ciphers (
            row_id       INTEGER PRIMARY KEY,
            user_id      TEXT NOT NULL COLLATE NOCASE,
            cipher_id    TEXT NOT NULL COLLATE NOCASE,
            organization_id TEXT NULL,
            folder_id    TEXT NULL COLLATE NOCASE,
            cipher_type  INTEGER NOT NULL,
            revision_date_unix_ms INTEGER NOT NULL,
            creation_date_unix_ms INTEGER NOT NULL,
            deleted_date_unix_ms INTEGER NULL,
            archived_date_unix_ms INTEGER NULL,
            favorite     INTEGER NOT NULL,
            reprompt     INTEGER NOT NULL,
            edit         INTEGER NOT NULL,
            view_password INTEGER NOT NULL,
            encrypted_key TEXT NULL,
            payload      BLOB NOT NULL,
        
            UNIQUE (user_id, cipher_id),
        
            FOREIGN KEY (user_id) REFERENCES accounts(user_id) ON DELETE CASCADE,
            FOREIGN KEY (user_id, folder_id) REFERENCES folders(user_id, folder_id)
                ON DELETE SET NULL
                DEFERRABLE INITIALLY DEFERRED
        );
        """;

    public void Initialize(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.OpenConnection();
        connection.Execute("""
                           PRAGMA foreign_keys = ON;
                           """);

        using var transaction = connection.BeginTransaction();
        connection.Execute(CreateTableSql, transaction: transaction);
        EnsureVaultMetadataColumns(connection, transaction);
        transaction.Commit();
    }

    private static void EnsureVaultMetadataColumns(IDbConnection connection, IDbTransaction transaction)
    {
        EnsureColumn(connection, transaction, "folders", "revision_date_unix_ms", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "folders", "encrypted_name", "TEXT NOT NULL DEFAULT ''");

        EnsureColumn(connection, transaction, "collections", "organization_id", "TEXT NULL");
        EnsureColumn(connection, transaction, "collections", "read_only", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "collections", "manage", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "collections", "hide_passwords", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "collections", "collection_type", "INTEGER NULL");
        EnsureColumn(connection, transaction, "collections", "encrypted_name", "TEXT NOT NULL DEFAULT ''");

        EnsureColumn(connection, transaction, "ciphers", "organization_id", "TEXT NULL");
        EnsureColumn(connection, transaction, "ciphers", "revision_date_unix_ms", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "ciphers", "creation_date_unix_ms", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "ciphers", "deleted_date_unix_ms", "INTEGER NULL");
        EnsureColumn(connection, transaction, "ciphers", "archived_date_unix_ms", "INTEGER NULL");
        EnsureColumn(connection, transaction, "ciphers", "favorite", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "ciphers", "reprompt", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "ciphers", "edit", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "ciphers", "view_password", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, transaction, "ciphers", "encrypted_key", "TEXT NULL");
    }

    private static void EnsureColumn(IDbConnection connection, IDbTransaction transaction, string tableName, string columnName, string columnDefinition)
    {
        var existingColumns = connection.Query<TableInfoRow>($"PRAGMA table_info({tableName});", transaction: transaction);
        if (existingColumns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        connection.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};", transaction: transaction);
    }

    private readonly record struct TableInfoRow(int Cid, string Name, string Type, int NotNull, string? DfltValue, int Pk);
}
