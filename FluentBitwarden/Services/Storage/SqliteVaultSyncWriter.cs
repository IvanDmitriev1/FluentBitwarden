using BitwaredApi.Abstractions;
using BitwaredApi.Models.Vault;
using Dapper;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultSyncWriter(IVaultDbConnectionFactory connectionFactory)
    : IVaultSyncWriter, IVaultAccountClearStore
{
    private const string DeleteCiphersSql = "DELETE FROM Ciphers WHERE AccountId = @AccountId;";
    private const string DeleteFoldersSql = "DELETE FROM Folders WHERE AccountId = @AccountId;";
    private const string DeleteCollectionsSql = "DELETE FROM Collections WHERE AccountId = @AccountId;";
    private const string DeleteSyncStateSql = "DELETE FROM SyncState WHERE AccountId = @AccountId;";
    private const string DeleteAccountsSql = "DELETE FROM Accounts WHERE AccountId = @AccountId;";

    public async ValueTask<IVaultSyncWriteSession> BeginReplaceAsync(
        VaultAccountRecord account,
        CancellationToken cancellationToken = default)
    {
        SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        SqliteTransaction transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await DeleteVaultRowsAsync(connection, transaction, account.AccountId, cancellationToken).ConfigureAwait(false);
            return new SqliteVaultSyncWriteSession(connection, transaction, account);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await transaction.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask ClearAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await DeleteVaultRowsAsync(connection, transaction, accountId, cancellationToken).ConfigureAwait(false);
        await ExecuteDeleteAsync(connection, transaction, DeleteSyncStateSql, accountId, cancellationToken).ConfigureAwait(false);
        await ExecuteDeleteAsync(connection, transaction, DeleteAccountsSql, accountId, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask DeleteVaultRowsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string accountId,
        CancellationToken cancellationToken)
    {
        await ExecuteDeleteAsync(connection, transaction, DeleteCiphersSql, accountId, cancellationToken).ConfigureAwait(false);
        await ExecuteDeleteAsync(connection, transaction, DeleteFoldersSql, accountId, cancellationToken).ConfigureAwait(false);
        await ExecuteDeleteAsync(connection, transaction, DeleteCollectionsSql, accountId, cancellationToken).ConfigureAwait(false);
    }

    private static ValueTask ExecuteDeleteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        string accountId,
        CancellationToken cancellationToken)
        => new(connection.ExecuteAsync(new CommandDefinition(
            sql,
            new AccountIdParameters(accountId),
            transaction,
            cancellationToken: cancellationToken)));

    private readonly record struct AccountIdParameters(string AccountId);

    private sealed class SqliteVaultSyncWriteSession(
        SqliteConnection connection,
        SqliteTransaction transaction,
        VaultAccountRecord account)
        : IVaultSyncWriteSession
    {
        private const string UpsertAccountSql =
            """
            INSERT INTO Accounts (AccountId, Email, ApiBase, IdentityBase, CreatedUtc, LastSyncUtc)
            VALUES (@AccountId, @Email, @ApiBase, @IdentityBase, @CreatedUtc, @LastSyncUtc)
            ON CONFLICT(AccountId) DO UPDATE SET
                Email = excluded.Email,
                ApiBase = excluded.ApiBase,
                IdentityBase = excluded.IdentityBase,
                LastSyncUtc = excluded.LastSyncUtc;
            """;

        private const string UpsertSyncStateSql =
            """
            INSERT INTO SyncState (AccountId, RevisionDate, LastSyncUtc, CipherCount, FolderCount, CollectionCount)
            VALUES (@AccountId, @RevisionDate, @LastSyncUtc, @CipherCount, @FolderCount, @CollectionCount)
            ON CONFLICT(AccountId) DO UPDATE SET
                RevisionDate = excluded.RevisionDate,
                LastSyncUtc = excluded.LastSyncUtc,
                CipherCount = excluded.CipherCount,
                FolderCount = excluded.FolderCount,
                CollectionCount = excluded.CollectionCount;
            """;

        public ValueTask WriteCipherAsync(
            CipherSyncItem item,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
            => WriteCipherCoreAsync(item, payload, cancellationToken);

        public ValueTask WriteFolderAsync(
            FolderSyncItem item,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
            => WriteNamedPayloadAsync(
                "Folders",
                """
                INSERT INTO Folders (AccountId, Id, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @RevisionDate, zeroblob(@PayloadLength), @UpdatedUtc);
                """,
                item.Id,
                item.RevisionDate,
                payload,
                cancellationToken);

        public ValueTask WriteCollectionAsync(
            CollectionSyncItem item,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
            => WriteNamedPayloadAsync(
                "Collections",
                """
                INSERT INTO Collections (AccountId, Id, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @RevisionDate, zeroblob(@PayloadLength), @UpdatedUtc);
                """,
                item.Id,
                item.RevisionDate,
                payload,
                cancellationToken);

        public async ValueTask CommitAsync(
            VaultSyncStateRecord syncState,
            CancellationToken cancellationToken = default)
        {
            await UpsertAccountAsync(cancellationToken).ConfigureAwait(false);
            await UpsertSyncStateAsync(syncState, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }

        private async ValueTask WriteCipherCoreAsync(
            CipherSyncItem item,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken)
        {
            await InsertCipherPlaceholderAsync(item, payload.Length, cancellationToken).ConfigureAwait(false);
            long rowId = await GetRowIdAsync("Ciphers", item.Id, cancellationToken).ConfigureAwait(false);
            WriteBlob("Ciphers", rowId, payload);
        }

        private async ValueTask WriteNamedPayloadAsync(
            string tableName,
            string sql,
            string id,
            DateTimeOffset? revisionDate,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken)
        {
            await InsertNamedPlaceholderAsync(sql, id, revisionDate, payload.Length, cancellationToken).ConfigureAwait(false);
            long rowId = await GetRowIdAsync(tableName, id, cancellationToken).ConfigureAwait(false);
            WriteBlob(tableName, rowId, payload);
        }

        private async ValueTask InsertCipherPlaceholderAsync(
            CipherSyncItem item,
            int payloadLength,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO Ciphers (AccountId, Id, Type, OrganizationId, FolderId, CollectionIdsJson, RevisionDate, EncJson, UpdatedUtc)
                VALUES (@AccountId, @Id, @Type, @OrganizationId, @FolderId, @CollectionIdsJson, @RevisionDate, zeroblob(@PayloadLength), @UpdatedUtc);
                """;
            command.Parameters.AddWithValue("@AccountId", account.AccountId);
            command.Parameters.AddWithValue("@Id", item.Id);
            command.Parameters.AddWithValue("@Type", item.Type);
            command.Parameters.AddWithValue("@OrganizationId", (object?)item.OrganizationId ?? DBNull.Value);
            command.Parameters.AddWithValue("@FolderId", (object?)item.FolderId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CollectionIdsJson", item.CollectionIdsJson);
            command.Parameters.AddWithValue("@RevisionDate", (object?)item.RevisionDate?.ToString("O") ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayloadLength", payloadLength);
            command.Parameters.AddWithValue("@UpdatedUtc", SyncUpdatedUtc.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask InsertNamedPlaceholderAsync(
            string sql,
            string id,
            DateTimeOffset? revisionDate,
            int payloadLength,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("@AccountId", account.AccountId);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@RevisionDate", (object?)revisionDate?.ToString("O") ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayloadLength", payloadLength);
            command.Parameters.AddWithValue("@UpdatedUtc", SyncUpdatedUtc.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<long> GetRowIdAsync(
            string tableName,
            string id,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"""
                SELECT rowid
                FROM {tableName}
                WHERE AccountId = @AccountId AND Id = @Id;
                """;
            command.Parameters.AddWithValue("@AccountId", account.AccountId);
            command.Parameters.AddWithValue("@Id", id);

            object? rowId = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return rowId switch
            {
                long value => value,
                int value => value,
                _ => throw new InvalidOperationException($"Inserted vault row '{tableName}/{id}' did not expose a rowid."),
            };
        }

        private void WriteBlob(string tableName, long rowId, ReadOnlyMemory<byte> payload)
        {
            using SqliteBlob blob = new(connection, tableName, "EncJson", rowId, readOnly: false);
            blob.Write(payload.Span);
        }

        private async ValueTask UpsertAccountAsync(CancellationToken cancellationToken)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UpsertAccountSql,
                new UpsertAccountParameters(
                    account.AccountId,
                    account.Email,
                    account.ApiBase,
                    account.IdentityBase,
                    account.CreatedUtc.ToString("O"),
                    account.LastSyncUtc?.ToString("O")),
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        private async ValueTask UpsertSyncStateAsync(
            VaultSyncStateRecord syncState,
            CancellationToken cancellationToken)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UpsertSyncStateSql,
                new UpsertSyncStateParameters(
                    syncState.AccountId,
                    syncState.RevisionDate?.ToString("O"),
                    syncState.LastSyncUtc.ToString("O"),
                    syncState.CipherCount,
                    syncState.FolderCount,
                    syncState.CollectionCount),
                transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        private DateTimeOffset SyncUpdatedUtc => account.LastSyncUtc
            ?? throw new InvalidOperationException("Vault account sync timestamp was not provided.");

        private readonly record struct UpsertAccountParameters(
            string AccountId,
            string Email,
            string ApiBase,
            string IdentityBase,
            string CreatedUtc,
            string? LastSyncUtc);

        private readonly record struct UpsertSyncStateParameters(
            string AccountId,
            string? RevisionDate,
            string LastSyncUtc,
            int CipherCount,
            int FolderCount,
            int CollectionCount);
    }
}
