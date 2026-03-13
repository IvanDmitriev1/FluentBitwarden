using BitwaredApi.Models.Vault;
using Dapper;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultSnapshotWriteStore(IVaultDbConnectionFactory connectionFactory) : IVaultSnapshotWriteStore
{
    public async ValueTask SaveSyncAsync(
        EncryptedSyncSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
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
                INSERT INTO SyncState (AccountId, RevisionDate, LastSyncUtc, CipherCount, FolderCount, CollectionCount)
                VALUES (@AccountId, @RevisionDate, @LastSyncUtc, @CipherCount, @FolderCount, @CollectionCount)
                ON CONFLICT(AccountId) DO UPDATE SET
                    RevisionDate = excluded.RevisionDate,
                    LastSyncUtc = excluded.LastSyncUtc,
                    CipherCount = excluded.CipherCount,
                    FolderCount = excluded.FolderCount,
                    CollectionCount = excluded.CollectionCount;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                upsertSyncState,
                snapshot.SyncState,
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            string accountId = snapshot.Account.AccountId;

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Ciphers WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Folders WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Collections WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string insertCipher = """
                INSERT INTO Ciphers (AccountId, Id, Type, OrganizationId, FolderId, CollectionIdsJson, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @Type, @OrganizationId, @FolderId, @CollectionIdsJson, @RevisionDate, @EncryptedPayload, @UpdatedUtc);
                """;

            if (snapshot.Ciphers.Count > 0)
            {
                CipherWriteRow[] cipherRows = [.. snapshot.Ciphers.Select(cipher => new CipherWriteRow(
                    accountId,
                    cipher.Id,
                    cipher.Type,
                    cipher.OrganizationId,
                    cipher.FolderId,
                    cipher.CollectionIdsJson,
                    cipher.RevisionDate,
                    cipher.EncryptedPayload,
                    snapshot.SyncState.LastSyncUtc))];

                await connection.ExecuteAsync(new CommandDefinition(
                    insertCipher,
                    cipherRows,
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            const string insertFolder = """
                INSERT INTO Folders (AccountId, Id, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @RevisionDate, @EncryptedPayload, @UpdatedUtc);
                """;

            if (snapshot.Folders.Count > 0)
            {
                FolderWriteRow[] folderRows = [.. snapshot.Folders.Select(folder => new FolderWriteRow(
                    accountId,
                    folder.Id,
                    folder.RevisionDate,
                    folder.EncryptedPayload,
                    snapshot.SyncState.LastSyncUtc))];

                await connection.ExecuteAsync(new CommandDefinition(
                    insertFolder,
                    folderRows,
                    transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            const string insertCollection = """
                INSERT INTO Collections (AccountId, Id, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @RevisionDate, @EncryptedPayload, @UpdatedUtc);
                """;

            if (snapshot.Collections.Count > 0)
            {
                CollectionWriteRow[] collectionRows = [.. snapshot.Collections.Select(collection => new CollectionWriteRow(
                    accountId,
                    collection.Id,
                    collection.RevisionDate,
                    collection.EncryptedPayload,
                    snapshot.SyncState.LastSyncUtc))];

                await connection.ExecuteAsync(new CommandDefinition(
                    insertCollection,
                    collectionRows,
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

    public async ValueTask ClearAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Ciphers WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Folders WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Collections WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM SyncState WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Accounts WHERE AccountId = @AccountId;",
                new { AccountId = accountId },
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private sealed record CipherWriteRow(
        string AccountId,
        string Id,
        int Type,
        string? OrganizationId,
        string? FolderId,
        string CollectionIdsJson,
        DateTimeOffset? RevisionDate,
        byte[] EncryptedPayload,
        DateTimeOffset UpdatedUtc);

    private sealed record FolderWriteRow(
        string AccountId,
        string Id,
        DateTimeOffset? RevisionDate,
        byte[] EncryptedPayload,
        DateTimeOffset UpdatedUtc);

    private sealed record CollectionWriteRow(
        string AccountId,
        string Id,
        DateTimeOffset? RevisionDate,
        byte[] EncryptedPayload,
        DateTimeOffset UpdatedUtc);
}
