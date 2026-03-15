using BitwaredApi.Models.Vault;
using Dapper;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultSyncStateStore(IVaultDbConnectionFactory connectionFactory) : IVaultSyncStateStore
{
    public async ValueTask<VaultSyncStateRecord?> GetByAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT AccountId, RevisionDate, LastSyncUtc, CipherCount, FolderCount, CollectionCount
            FROM SyncState
            WHERE AccountId = @AccountId;
            """;

        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        VaultSyncStateRow? row = await connection.QuerySingleOrDefaultAsync<VaultSyncStateRow>(
            new CommandDefinition(sql, new AccountIdParameters(accountId), cancellationToken: cancellationToken)).ConfigureAwait(false);

        return row is null ? null : MapSyncStateRow(row);
    }

    private static VaultSyncStateRecord MapSyncStateRow(VaultSyncStateRow row)
        => new(
            row.AccountId,
            SqliteVaultValueParser.ParseNullableDate(row.RevisionDate, nameof(VaultSyncStateRow.RevisionDate)),
            SqliteVaultValueParser.ParseRequiredDate(row.LastSyncUtc, nameof(VaultSyncStateRow.LastSyncUtc)),
            checked((int)row.CipherCount),
            checked((int)row.FolderCount),
            checked((int)row.CollectionCount));

    private sealed record VaultSyncStateRow(
        string AccountId,
        string? RevisionDate,
        string LastSyncUtc,
        long CipherCount,
        long FolderCount,
        long CollectionCount);

    private readonly record struct AccountIdParameters(string AccountId);
}
