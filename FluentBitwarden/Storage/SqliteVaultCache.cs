using BitwaredApi.Models.Vault;
using Dapper;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Core.Abstractions;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace FluentBitwarden.Storage;

public sealed class SqliteVaultCache : IVaultCache
{
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
            LastSyncUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Ciphers (
            AccountId TEXT NOT NULL,
            Id TEXT NOT NULL,
            Type INTEGER NOT NULL,
            OrganizationId TEXT NULL,
            FolderId TEXT NULL,
            CollectionIdsJson TEXT NOT NULL,
            RevisionDate TEXT NULL,
            EncJson TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (AccountId, Id)
        );

        CREATE INDEX IF NOT EXISTS IX_Ciphers_AccountId_UpdatedUtc ON Ciphers (AccountId, UpdatedUtc DESC);

        CREATE TABLE IF NOT EXISTS Folders (
            AccountId TEXT NOT NULL,
            Id TEXT NOT NULL,
            RevisionDate TEXT NULL,
            EncJson TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (AccountId, Id)
        );

        CREATE TABLE IF NOT EXISTS Collections (
            AccountId TEXT NOT NULL,
            Id TEXT NOT NULL,
            RevisionDate TEXT NULL,
            EncJson TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (AccountId, Id)
        );
        """;

    private readonly IAppPaths _paths;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;

    public SqliteVaultCache(IAppPaths paths)
    {
        Batteries_V2.Init();
        _paths = paths;
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializeGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_initialized)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_paths.VaultDbFilePath)!);

            await using SqliteConnection connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(Schema, cancellationToken: cancellationToken)).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public async ValueTask SaveSyncAsync(EncryptedSyncSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            const string upsertAccount = """
                INSERT INTO Accounts (AccountId, Email, ApiBase, IdentityBase, CreatedUtc, LastSyncUtc)
                VALUES (@AccountId, @Email, @ApiBase, @IdentityBase, @CreatedUtc, @LastSyncUtc)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Email = excluded.Email,
                    ApiBase = excluded.ApiBase,
                    IdentityBase = excluded.IdentityBase,
                    LastSyncUtc = excluded.LastSyncUtc;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                upsertAccount,
                snapshot.Account,
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string upsertSyncState = """
                INSERT INTO SyncState (AccountId, RevisionDate, LastSyncUtc)
                VALUES (@AccountId, @RevisionDate, @LastSyncUtc)
                ON CONFLICT(AccountId) DO UPDATE SET
                    RevisionDate = excluded.RevisionDate,
                    LastSyncUtc = excluded.LastSyncUtc;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                upsertSyncState,
                snapshot.SyncState,
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Ciphers WHERE AccountId = @AccountId;",
                new { snapshot.Account.AccountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Folders WHERE AccountId = @AccountId;",
                new { snapshot.Account.AccountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Collections WHERE AccountId = @AccountId;",
                new { snapshot.Account.AccountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string insertCipher = """
                INSERT INTO Ciphers (AccountId, Id, Type, OrganizationId, FolderId, CollectionIdsJson, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @Type, @OrganizationId, @FolderId, @CollectionIdsJson, @RevisionDate, @EncryptedJson, @UpdatedUtc);
                """;

            if (snapshot.Ciphers.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    insertCipher,
                    snapshot.Ciphers,
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            const string insertFolder = """
                INSERT INTO Folders (AccountId, Id, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @RevisionDate, @EncryptedJson, @UpdatedUtc);
                """;

            if (snapshot.Folders.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    insertFolder,
                    snapshot.Folders,
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            const string insertCollection = """
                INSERT INTO Collections (AccountId, Id, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @RevisionDate, @EncryptedJson, @UpdatedUtc);
                """;

            if (snapshot.Collections.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    insertCollection,
                    snapshot.Collections,
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask<IReadOnlyList<EncryptedCipherRecord>> ListCiphersAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT
                AccountId,
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson AS EncryptedJson,
                UpdatedUtc
            FROM Ciphers
            WHERE AccountId = @AccountId
            ORDER BY UpdatedUtc DESC;
            """;

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<EncryptedCipherRow> rows = (await connection.QueryAsync<EncryptedCipherRow>(
            new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken)).ConfigureAwait(false)).AsList();
        return rows.Select(MapCipherRow).ToList();
    }

    public async ValueTask<EncryptedCipherRecord?> GetCipherAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT
                AccountId,
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson AS EncryptedJson,
                UpdatedUtc
            FROM Ciphers
            WHERE AccountId = @AccountId AND Id = @Id;
            """;

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        EncryptedCipherRow? row = await connection.QuerySingleOrDefaultAsync<EncryptedCipherRow>(
            new CommandDefinition(sql, new { AccountId = accountId, Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row is null ? null : MapCipherRow(row);
    }

    public async ValueTask<VaultSyncStateRecord?> GetSyncStateAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT AccountId, RevisionDate, LastSyncUtc
            FROM SyncState
            WHERE AccountId = @AccountId;
            """;

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        VaultSyncStateRow? row = await connection.QuerySingleOrDefaultAsync<VaultSyncStateRow>(
            new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row is null ? null : MapSyncStateRow(row);
    }

    public async ValueTask ClearAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Ciphers WHERE AccountId = @AccountId;", new { AccountId = accountId }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Folders WHERE AccountId = @AccountId;", new { AccountId = accountId }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Collections WHERE AccountId = @AccountId;", new { AccountId = accountId }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM SyncState WHERE AccountId = @AccountId;", new { AccountId = accountId }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Accounts WHERE AccountId = @AccountId;", new { AccountId = accountId }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private SqliteConnection CreateConnection()
        => new(new SqliteConnectionStringBuilder
        {
            DataSource = _paths.VaultDbFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString());

    private static EncryptedCipherRecord MapCipherRow(EncryptedCipherRow row)
        => new(
            row.AccountId,
            row.Id,
            checked((int)row.Type),
            row.OrganizationId,
            row.FolderId,
            row.CollectionIdsJson,
            ParseNullableDate(row.RevisionDate, nameof(EncryptedCipherRow.RevisionDate)),
            row.EncryptedJson,
            ParseRequiredDate(row.UpdatedUtc, nameof(EncryptedCipherRow.UpdatedUtc)));

    private static VaultSyncStateRecord MapSyncStateRow(VaultSyncStateRow row)
        => new(
            row.AccountId,
            ParseNullableDate(row.RevisionDate, nameof(VaultSyncStateRow.RevisionDate)),
            ParseRequiredDate(row.LastSyncUtc, nameof(VaultSyncStateRow.LastSyncUtc)));

    private static DateTimeOffset? ParseNullableDate(string? value, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Stored SQLite value '{columnName}' could not be parsed as DateTimeOffset.");
    }

    private static DateTimeOffset ParseRequiredDate(string value, string columnName)
    {
        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Stored SQLite value '{columnName}' could not be parsed as DateTimeOffset.");
    }

    private sealed record EncryptedCipherRow(
        string AccountId,
        string Id,
        long Type,
        string? OrganizationId,
        string? FolderId,
        string CollectionIdsJson,
        string? RevisionDate,
        string EncryptedJson,
        string UpdatedUtc);

    private sealed record VaultSyncStateRow(
        string AccountId,
        string? RevisionDate,
        string LastSyncUtc);
}
