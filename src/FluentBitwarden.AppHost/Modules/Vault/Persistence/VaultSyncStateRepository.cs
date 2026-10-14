using Dapper;
using FluentBitwarden.AppHost.Infrastructure.Data;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence;

internal sealed class VaultSyncStateRepository(IDbSession dbSession)
{
    public DateTimeOffset? GetServerRevisionDate(UserId userId)
    {
        userId.ThrowIfEmpty();

        long? revisionDateUnixMs = dbSession.Connection.ExecuteScalar<long?>(
            """
            SELECT server_revision_date_unix_ms
            FROM vault_sync_state
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new { UserId = userId.ToString() },
            transaction: dbSession.Transaction);

        return revisionDateUnixMs.ToDateTimeOffsetFromUnixMs();
    }

    public void UpsertServerRevisionDate(UserId userId, DateTimeOffset revisionDate)
    {
        userId.ThrowIfEmpty();

        dbSession.Connection.Execute(
            """
            INSERT INTO vault_sync_state (user_id, server_revision_date_unix_ms)
            VALUES (@UserId, @RevisionDateUnixMs)
            ON CONFLICT(user_id) DO UPDATE SET
                server_revision_date_unix_ms = excluded.server_revision_date_unix_ms;
            """,
            new
            {
                UserId = userId.ToString(),
                RevisionDateUnixMs = revisionDate.ToUnixMs()
            },
            transaction: dbSession.RequiredTransaction);
    }
}
