using Dapper;
using FluentBitwarden.Data.Abstractions;

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
            row_id   INTEGER PRIMARY KEY,
            user_id   TEXT NOT NULL COLLATE NOCASE,
            folder_id TEXT NOT NULL COLLATE NOCASE,
            payload   BLOB NOT NULL,
        
            UNIQUE (user_id, folder_id),
        
            FOREIGN KEY (user_id) REFERENCES accounts(user_id) ON DELETE CASCADE
        );
        
        CREATE TABLE IF NOT EXISTS collections (
            row_id         INTEGER PRIMARY KEY,
            user_id        TEXT NOT NULL COLLATE NOCASE,
            collection_id  TEXT NOT NULL COLLATE NOCASE,
            payload        BLOB NOT NULL,
        
            UNIQUE (user_id, collection_id),
        
            FOREIGN KEY (user_id) REFERENCES accounts(user_id) ON DELETE CASCADE
        );
        
        CREATE TABLE IF NOT EXISTS ciphers (
            row_id       INTEGER PRIMARY KEY,
            user_id      TEXT NOT NULL COLLATE NOCASE,
            cipher_id    TEXT NOT NULL COLLATE NOCASE,
            folder_id    TEXT NULL COLLATE NOCASE,
            cipher_type  INTEGER NOT NULL,
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
        transaction.Commit();
    }
}
