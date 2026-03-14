using BitwaredApi.Models.Vault;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultCipherReadStore(IVaultDbConnectionFactory connectionFactory) : IVaultCipherReadStore
{
    public async ValueTask VisitByAccountAsync(
        string accountId,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask<bool>> visitAsync,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT
                rowid,
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson
            FROM Ciphers
            WHERE AccountId = @AccountId
            ORDER BY UpdatedUtc DESC;
            """;

        await using var connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@AccountId", accountId);

        await using SqliteDataReader reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            CipherSyncItem item = MapCipherRow(reader);
            await using Stream payload = reader.GetStream(7);

            if (!await visitAsync(item, payload, cancellationToken).ConfigureAwait(false))
            {
                break;
            }
        }
    }

    public async ValueTask<bool> VisitByIdAsync(
        string accountId,
        string id,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask> visitAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visitAsync);

        const string sql =
            """
            SELECT
                rowid,
                Id,
                Type,
                OrganizationId,
                FolderId,
                CollectionIdsJson,
                RevisionDate,
                EncJson
            FROM Ciphers
            WHERE AccountId = @AccountId AND Id = @Id;
            """;

        await using var connection = await connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@AccountId", accountId);
        command.Parameters.AddWithValue("@Id", id);

        await using SqliteDataReader reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var item = MapCipherRow(reader);
        await using var payload = reader.GetStream(7);
        await visitAsync(item, payload, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static CipherSyncItem MapCipherRow(SqliteDataReader reader)
        => new(
            reader.GetString(1),
            checked((int)reader.GetInt64(2)),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            SqliteVaultValueParser.ParseNullableDate(
                reader.IsDBNull(6) ? null : reader.GetString(6),
                "RevisionDate"));
}
