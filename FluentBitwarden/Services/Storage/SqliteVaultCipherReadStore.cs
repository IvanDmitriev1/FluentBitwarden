using BitwaredApi.Models.Vault;
using Dapper;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultCipherReadStore(IVaultDbConnectionFactory connectionFactory) : IVaultCipherReadStore
{
    public async ValueTask<IReadOnlyList<CipherSyncItem>> ListByAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson AS EncryptedPayload
            FROM Ciphers
            WHERE AccountId = @AccountId
            ORDER BY UpdatedUtc DESC;
            """;

        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CipherRow> rows = (await connection.QueryAsync<CipherRow>(
            new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken)).ConfigureAwait(false)).AsList();

        return rows.Select(MapCipherRow).ToList();
    }

    public async ValueTask<CipherSyncItem?> GetByIdAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson AS EncryptedPayload
            FROM Ciphers
            WHERE AccountId = @AccountId AND Id = @Id;
            """;

        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        CipherRow? row = await connection.QuerySingleOrDefaultAsync<CipherRow>(
            new CommandDefinition(sql, new { AccountId = accountId, Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return row is null ? null : MapCipherRow(row);
    }

    private static CipherSyncItem MapCipherRow(CipherRow row)
        => new(
            row.Id,
            checked((int)row.Type),
            row.OrganizationId,
            row.FolderId,
            row.CollectionIdsJson,
            SqliteVaultValueParser.ParseNullableDate(row.RevisionDate, nameof(CipherRow.RevisionDate)),
            row.EncryptedPayload);

    private sealed record CipherRow(
        string Id,
        long Type,
        string? OrganizationId,
        string? FolderId,
        string CollectionIdsJson,
        string? RevisionDate,
        byte[] EncryptedPayload);
}
