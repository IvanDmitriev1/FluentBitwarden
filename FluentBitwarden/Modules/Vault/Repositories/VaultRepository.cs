using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.HighPerformance.Buffers;
using Dapper;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Vault.Abstractions;
using Microsoft.Data.Sqlite;
using System.Linq;
using static FluentBitwarden.Modules.Vault.Abstractions.IVaultRepository;

namespace FluentBitwarden.Modules.Vault.Repositories;

internal sealed partial class VaultRepository(SqliteTransaction transaction) : BaseRepository(transaction), IVaultRepository
{
    public IEnumerable<FolderDto> GetAllFolders(UserId userId)
    {
        var rows = Connection.Query<FolderRow>(
            """
            SELECT
                folder_id,
                revision_date_unix_ms,
                encrypted_name
            FROM folders
            WHERE user_id = @UserId
            ORDER BY row_id;
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);

        return rows.Select(static row => ToDto(row));
    }

    public IEnumerable<CollectionDto> GetAllCollections(UserId userId)
    {
        var rows = Connection.Query<CollectionRow>(
            """
            SELECT
                collection_id,
                organization_id,
                read_only,
                manage,
                hide_passwords,
                collection_type,
                encrypted_name
            FROM collections
            WHERE user_id = @UserId
            ORDER BY row_id;
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);

        return rows.Select(static row => ToDto(row));
    }

    public void ReadAllCiphers<TState>(UserId userId, TState stateObj, CipherVisitor<TState> onCipher)
    {
        var cipherRows = Connection.Query<CipherRow>(
            """
            SELECT
                row_id,
                cipher_id,
                organization_id,
                folder_id,
                encrypted_key,
                cipher_type,
                revision_date_unix_ms,
                creation_date_unix_ms,
                deleted_date_unix_ms,
                archived_date_unix_ms,
                favorite,
                reprompt,
                edit,
                view_password
            FROM ciphers
            WHERE user_id = @UserId
            ORDER BY row_id;
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);


        int? bufferLength = Connection.ExecuteScalar<int?>(
            """
            SELECT MAX(length(payload))
            FROM ciphers
            WHERE user_id = @UserId
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);

        if (bufferLength is null)
            return;
        
        using var bufferOwner = SpanOwner<byte>.Allocate(bufferLength.Value);

        foreach (var row in cipherRows)
        {
            using var blob = new SqliteBlob(Connection, "ciphers", "payload", row.RowId, readOnly: true);
            bufferOwner.Span.Clear();
            int bytesWritten = blob.Read(bufferOwner.Span);

            var dto = ToDto(row);
            onCipher.Invoke(stateObj, ref dto, bufferOwner.Span[..bytesWritten]);
        }
    }


}
