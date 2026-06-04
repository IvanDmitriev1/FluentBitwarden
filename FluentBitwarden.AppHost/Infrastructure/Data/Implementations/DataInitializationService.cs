using Dapper;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class DataInitializationService(ISqliteConnectionFactory connectionFactory) : IDataInitializationService
{
    private const string CreateTableSql =
        """
        CREATE TABLE IF NOT EXISTS account_profiles (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
            email TEXT NOT NULL CHECK (length(email) > 0),
            api_base TEXT NOT NULL CHECK (length(api_base) > 0),
            identity_base TEXT NOT NULL CHECK (length(identity_base) > 0),
            notifications_base TEXT NOT NULL CHECK (length(notifications_base) > 0),
            vault_base TEXT NOT NULL CHECK (length(vault_base) > 0),
            last_sync_at_unix_ms INTEGER NOT NULL CHECK (last_sync_at_unix_ms >= 0)
        );
        
        CREATE TABLE IF NOT EXISTS account_key_material (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
            salt TEXT NOT NULL CHECK (length(salt) > 0),
            encrypted_user_key BLOB NOT NULL CHECK (length(encrypted_user_key) > 0),
            encrypted_private_key BLOB NOT NULL CHECK (length(encrypted_private_key) > 0),
            kdf_type INTEGER NOT NULL CHECK (kdf_type IN (0, 1)),
            kdf_iterations INTEGER NOT NULL CHECK (kdf_iterations > 0),
            kdf_memory_mib INTEGER,
            kdf_parallelism INTEGER,

            CHECK (
                (kdf_type = 0 AND kdf_memory_mib IS NULL AND kdf_parallelism IS NULL) OR
                (
                    kdf_type = 1 AND
                    kdf_memory_mib IS NOT NULL AND kdf_memory_mib > 0 AND
                    kdf_parallelism IS NOT NULL AND kdf_parallelism > 0
                )
            )
        );

        CREATE TABLE IF NOT EXISTS account_session_tokens (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
            protected_refresh_token BLOB NOT NULL CHECK (length(protected_refresh_token) > 0)
        );

        CREATE TABLE IF NOT EXISTS account_tpm_cng_unlock_keys (
            user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE,
            protected_user_key BLOB NOT NULL CHECK (length(protected_user_key) > 0)
        );

        CREATE TABLE IF NOT EXISTS vault_organization (
            row_id INTEGER PRIMARY KEY,
            user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
            organization_id TEXT NOT NULL COLLATE NOCASE CHECK (length(organization_id) > 0),
            organization_user_id TEXT NULL COLLATE NOCASE CHECK (organization_user_id IS NULL OR length(organization_user_id) > 0),
            organization_name TEXT NOT NULL CHECK (length(organization_name) > 0),
            is_enabled INTEGER NOT NULL CHECK (is_enabled IN (0, 1)),
            use_key_connector INTEGER NOT NULL CHECK (use_key_connector IN (0, 1)),
            member_status INTEGER NULL CHECK (member_status IS NULL OR member_status IN (-1, 0, 1, 2)),
            member_type INTEGER NULL CHECK (member_type IS NULL OR member_type IN (0, 1, 2, 4)),
            encrypted_organization_key BLOB NULL CHECK (encrypted_organization_key IS NULL OR length(encrypted_organization_key) > 0),

            UNIQUE (user_id, organization_id)
        );
        
        CREATE TABLE IF NOT EXISTS vault_folder (
            row_id INTEGER PRIMARY KEY,
            user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
            folder_id TEXT NOT NULL COLLATE NOCASE CHECK (length(folder_id) > 0),
            revision_date_unix_ms INTEGER NOT NULL CHECK (revision_date_unix_ms >= 0),
            encrypted_name BLOB NOT NULL CHECK (length(encrypted_name) > 0),
        
            UNIQUE (user_id, folder_id)
        );
        
        CREATE TABLE IF NOT EXISTS vault_collection (
            row_id INTEGER PRIMARY KEY,
            user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
            collection_id TEXT NOT NULL COLLATE NOCASE CHECK (length(collection_id) > 0),
            organization_id TEXT NULL COLLATE NOCASE CHECK (organization_id IS NULL OR length(organization_id) > 0),
            is_read_only INTEGER NOT NULL CHECK (is_read_only IN (0, 1)),
            can_manage INTEGER NOT NULL CHECK (can_manage IN (0, 1)),
            hide_passwords INTEGER NOT NULL CHECK (hide_passwords IN (0, 1)),
            collection_type INTEGER NULL,
            encrypted_name BLOB NOT NULL CHECK (length(encrypted_name) > 0),
        
            UNIQUE (user_id, collection_id),

            FOREIGN KEY (user_id, organization_id) REFERENCES vault_organization(user_id, organization_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
        );
        
        CREATE TABLE IF NOT EXISTS vault_cipher (
            row_id INTEGER PRIMARY KEY,
            user_id TEXT NOT NULL COLLATE NOCASE REFERENCES account_profiles(user_id) ON DELETE CASCADE CHECK (length(user_id) > 0),
            cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
            organization_id TEXT NULL COLLATE NOCASE CHECK (organization_id IS NULL OR length(organization_id) > 0),
            cipher_type INTEGER NOT NULL CHECK (cipher_type IN (1, 2, 3, 4, 5)),
            revision_date_unix_ms INTEGER NOT NULL CHECK (revision_date_unix_ms >= 0),
            creation_date_unix_ms INTEGER NOT NULL CHECK (creation_date_unix_ms >= 0),
            deleted_date_unix_ms INTEGER NULL CHECK (deleted_date_unix_ms IS NULL OR deleted_date_unix_ms >= 0),
            archived_date_unix_ms INTEGER NULL CHECK (archived_date_unix_ms IS NULL OR archived_date_unix_ms >= 0),
            is_favorite INTEGER NOT NULL CHECK (is_favorite IN (0, 1)),
            reprompt INTEGER NOT NULL CHECK (reprompt IN (0, 1)),
            can_edit INTEGER NOT NULL CHECK (can_edit IN (0, 1)),
            can_view_password INTEGER NOT NULL CHECK (can_view_password IN (0, 1)),
            encrypted_cipher_key BLOB NULL CHECK (encrypted_cipher_key IS NULL OR length(encrypted_cipher_key) > 0),
            encrypted_payload BLOB NOT NULL CHECK (length(encrypted_payload) > 0),
        
            UNIQUE (user_id, cipher_id),

            FOREIGN KEY (user_id, organization_id) REFERENCES vault_organization(user_id, organization_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
        );

        CREATE TABLE IF NOT EXISTS vault_cipher_folder (
            user_id TEXT NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
            cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
            folder_id TEXT NOT NULL COLLATE NOCASE CHECK (length(folder_id) > 0),

            PRIMARY KEY (user_id, cipher_id),

            FOREIGN KEY (user_id, cipher_id) REFERENCES vault_cipher(user_id, cipher_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,
            FOREIGN KEY (user_id, folder_id) REFERENCES vault_folder(user_id, folder_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
        );

        CREATE TABLE IF NOT EXISTS vault_cipher_collection (
            user_id TEXT NOT NULL COLLATE NOCASE CHECK (length(user_id) > 0),
            cipher_id TEXT NOT NULL COLLATE NOCASE CHECK (length(cipher_id) > 0),
            collection_id TEXT NOT NULL COLLATE NOCASE CHECK (length(collection_id) > 0),

            PRIMARY KEY (user_id, cipher_id, collection_id),

            FOREIGN KEY (user_id, cipher_id) REFERENCES vault_cipher(user_id, cipher_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED,
            FOREIGN KEY (user_id, collection_id) REFERENCES vault_collection(user_id, collection_id) ON DELETE CASCADE DEFERRABLE INITIALLY DEFERRED
        );

        CREATE INDEX IF NOT EXISTS ix_vault_collection_organization
            ON vault_collection(user_id, organization_id);

        CREATE INDEX IF NOT EXISTS ix_vault_cipher_organization
            ON vault_cipher(user_id, organization_id);
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
