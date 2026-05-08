using Dapper;
using FluentBitwarden.Data.Abstractions;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace FluentBitwarden.Data.Implementations;

internal sealed class DataInitializationService(ISqliteConnectionFactory connectionFactory) : IDataInitializationService
{
    private const string CreateTableSql =
        """
        CREATE TABLE IF NOT EXISTS account_profiles (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE,
            email TEXT NOT NULL,
            api_base TEXT NOT NULL,
            identity_base TEXT NOT NULL,
            notifications_base TEXT NOT NULL,
            vault_base TEXT NOT NULL,
            last_sync_at_unix_ms INTEGER NOT NULL,
            available_unlock_methods INTEGER NOT NULL DEFAULT 1
        );
        
        CREATE TABLE IF NOT EXISTS account_key_material (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
            salt TEXT NOT NULL,
            encrypted_user_key TEXT NOT NULL,
            encrypted_private_key TEXT NOT NULL,
            kdf_type INTEGER NOT NULL,
            kdf_iterations INTEGER NOT NULL,
            kdf_memory_mib INTEGER,
            kdf_parallelism INTEGER
        );

        CREATE TABLE IF NOT EXISTS account_session_tokens (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
            protected_refresh_token BLOB NOT NULL
        );

        CREATE TABLE IF NOT EXISTS account_tpm_cng_unlock_keys (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
            protected_user_key BLOB NOT NULL
        );
        
        CREATE TABLE IF NOT EXISTS folders (
            row_id             INTEGER PRIMARY KEY,
            user_id   TEXT NOT NULL COLLATE NOCASE,
            folder_id TEXT NOT NULL COLLATE NOCASE,
            revision_date_unix_ms INTEGER NOT NULL,
            encrypted_name    TEXT NOT NULL,
        
            UNIQUE (user_id, folder_id),
        
            FOREIGN KEY (user_id) REFERENCES account_profiles(user_id) ON DELETE CASCADE
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
        
            FOREIGN KEY (user_id) REFERENCES account_profiles(user_id) ON DELETE CASCADE
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
        
            FOREIGN KEY (user_id) REFERENCES account_profiles(user_id) ON DELETE CASCADE,
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
                           PRAGMA journal_mode = WAL;
                           PRAGMA synchronous = NORMAL;
                           PRAGMA busy_timeout = 5000;
                           """);

        using var transaction = connection.BeginTransaction();
        connection.Execute(CreateTableSql, transaction: transaction);
        transaction.Commit();
    }
}
