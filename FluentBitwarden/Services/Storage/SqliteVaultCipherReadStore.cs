using BitwaredApi.Models.Vault;
using Dapper;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultCipherReadStore(IVaultDbConnectionFactory connectionFactory) : IVaultCipherReadStore
{
    public async ValueTask<IReadOnlyList<EncryptedCipherRecord>> ListByAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                AccountId,
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson AS EncryptedPayload,
                UpdatedUtc
            FROM Ciphers
            WHERE AccountId = @AccountId
            ORDER BY UpdatedUtc DESC;
            """;

        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<EncryptedCipherRow> rows = (await connection.QueryAsync<EncryptedCipherRow>(
            new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken)).ConfigureAwait(false)).AsList();

        return rows.Select(MapCipherRow).ToList();
    }

    public async ValueTask<EncryptedCipherRecord?> GetByIdAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                AccountId,
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson AS EncryptedPayload,
                UpdatedUtc
            FROM Ciphers
            WHERE AccountId = @AccountId AND Id = @Id;
            """;

        await using SqliteConnection connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        EncryptedCipherRow? row = await connection.QuerySingleOrDefaultAsync<EncryptedCipherRow>(
            new CommandDefinition(sql, new { AccountId = accountId, Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return row is null ? null : MapCipherRow(row);
    }

    private static EncryptedCipherRecord MapCipherRow(EncryptedCipherRow row)
        => new(
            row.AccountId,
            row.Id,
            checked((int)row.Type),
            row.OrganizationId,
            row.FolderId,
            row.CollectionIdsJson,
            SqliteVaultValueParser.ParseNullableDate(row.RevisionDate, nameof(EncryptedCipherRow.RevisionDate)),
            row.EncryptedPayload,
            SqliteVaultValueParser.ParseRequiredDate(row.UpdatedUtc, nameof(EncryptedCipherRow.UpdatedUtc)));

    private sealed record EncryptedCipherRow(
        string AccountId,
        string Id,
        long Type,
        string? OrganizationId,
        string? FolderId,
        string CollectionIdsJson,
        string? RevisionDate,
        byte[] EncryptedPayload,
        string UpdatedUtc);
}
