using Dapper;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteDbInitializerService(
    IAppPaths paths,
    IVaultDbConnectionFactory connectionFactory)
    : IDbInitializerService
{
    private const int SchemaVersion = 2;
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Accounts (
            AccountId TEXT NOT NULL PRIMARY KEY,
            Email TEXT NOT NULL,
            ApiBase TEXT NOT NULL,
            IdentityBase TEXT NOT NULL,
            CreatedUtc TEXT NOT NULL,
            LastSyncUtc TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS SyncState (
            AccountId TEXT NOT NULL PRIMARY KEY,
            RevisionDate TEXT NULL,
            LastSyncUtc TEXT NOT NULL,
            CipherCount INTEGER NOT NULL,
            FolderCount INTEGER NOT NULL,
            CollectionCount INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Ciphers (
            AccountId TEXT NOT NULL,
            Id TEXT NOT NULL,
            Type INTEGER NOT NULL,
            OrganizationId TEXT NULL,
            FolderId TEXT NULL,
            CollectionIdsJson TEXT NOT NULL,
            RevisionDate TEXT NULL,
            EncJson BLOB NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (AccountId, Id)
        );

        CREATE INDEX IF NOT EXISTS IX_Ciphers_AccountId_UpdatedUtc ON Ciphers (AccountId, UpdatedUtc DESC);

        CREATE TABLE IF NOT EXISTS Folders (
            AccountId TEXT NOT NULL,
            Id TEXT NOT NULL,
            RevisionDate TEXT NULL,
            EncJson BLOB NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (AccountId, Id)
        );

        CREATE TABLE IF NOT EXISTS Collections (
            AccountId TEXT NOT NULL,
            Id TEXT NOT NULL,
            RevisionDate TEXT NULL,
            EncJson BLOB NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (AccountId, Id)
        );
        """;

    private bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(paths.VaultDbFilePath)!);
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    private async ValueTask EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        bool recreateDatabase = false;

        if (File.Exists(paths.VaultDbFilePath))
        {
            try
            {
                await using SqliteConnection versionConnection = await connectionFactory
                    .OpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false);

                long userVersion = await versionConnection.ExecuteScalarAsync<long>(
                    new CommandDefinition("PRAGMA user_version;", cancellationToken: cancellationToken)).ConfigureAwait(false);

                recreateDatabase = userVersion != SchemaVersion;
            }
            catch (SqliteException)
            {
                recreateDatabase = true;
            }
        }

        if (recreateDatabase)
        {
            File.Delete(paths.VaultDbFilePath);
        }

        await using var connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition(Schema, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition($"PRAGMA user_version = {SchemaVersion};", cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
