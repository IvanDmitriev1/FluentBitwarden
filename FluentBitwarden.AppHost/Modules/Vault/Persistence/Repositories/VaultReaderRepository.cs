using CommunityToolkit.HighPerformance.Buffers;
using Dapper;
using BitwardenApi.Vault.Attachments.Contracts;
using FluentBitwarden.AppHost.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;

internal sealed partial class VaultReaderRepository(SqliteTransaction transaction) : BaseRepository(transaction)
{
    public delegate void CipherVisitor<in TState>(
        TState state,
        ref readonly VaultCipherDto dto,
        ReadOnlySpan<byte> payload);

    public VaultFolderDto[] GetAllFolders(UserId userId)
    {
        var rows = Connection.Query<FolderRow>(
            """
            SELECT
                folder_id,
                revision_date_unix_ms,
                encrypted_name
            FROM vault_folder
            WHERE user_id = @UserId
            ORDER BY row_id;
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);

        return rows.Select(static row => ToDto(row)).ToArray();
    }

    public VaultOrganizationDto[] GetAllOrganizations(UserId userId)
    {
        var rows = Connection.Query<OrganizationRow>(
            """
            SELECT
                user_id,
                organization_id,
                organization_user_id,
                organization_name,
                is_enabled,
                access_secrets_manager,
                member_status,
                encrypted_organization_key
            FROM vault_organization
            WHERE user_id = @UserId
            ORDER BY row_id;
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);

        return rows.Select(static row => ToDto(row)).ToArray();
    }

    public VaultCollectionDto[] GetAllCollections(UserId userId)
    {
        var rows = Connection.Query<CollectionRow>(
            """
            SELECT
                collection_id,
                organization_id,
                is_read_only,
                can_manage,
                hide_passwords,
                collection_type,
                encrypted_name
            FROM vault_collection
            WHERE user_id = @UserId
            ORDER BY row_id;
            """,
            new { UserId = userId.ToString() },
            transaction: Transaction);

        return rows.Select(static row => ToDto(row)).ToArray();
    }

    public void ReadAllCiphers<TState>(UserId userId, TState stateObj, CipherVisitor<TState> onCipher)
    {
        var userIdString = userId.ToString();
        var collectionIdsByCipherId = GetCollectionIdsByCipherId(userIdString);
        var attachmentsByCipherId = GetAttachmentsByCipherId(userIdString);
        var cipherRows = Connection.Query<CipherRow>(
            """
            SELECT
                vc.row_id,
                vc.cipher_id,
                vc.organization_id,
                vcf.folder_id,
                vc.encrypted_cipher_key,
                vc.cipher_type,
                vc.revision_date_unix_ms,
                vc.creation_date_unix_ms,
                vc.deleted_date_unix_ms,
                vc.archived_date_unix_ms,
                vc.is_favorite,
                vc.reprompt,
                vc.can_edit,
                vc.can_view_password
            FROM vault_cipher vc
            LEFT JOIN vault_cipher_folder vcf
                ON vcf.user_id = vc.user_id
                AND vcf.cipher_id = vc.cipher_id
            WHERE vc.user_id = @UserId
            ORDER BY vc.row_id;
            """,
            new { UserId = userIdString },
            transaction: Transaction);


        int? bufferLength = Connection.ExecuteScalar<int?>(
            """
            SELECT MAX(length(encrypted_payload))
            FROM vault_cipher
            WHERE user_id = @UserId
            """,
            new { UserId = userIdString },
            transaction: Transaction);

        if (bufferLength is null)
            return;

        using var bufferOwner = SpanOwner<byte>.Allocate(bufferLength.Value);

        foreach (var row in cipherRows)
        {
            using var blob = new SqliteBlob(Connection, "vault_cipher", "encrypted_payload", row.RowId, readOnly: true);
            bufferOwner.Span.Clear();
            int bytesWritten = blob.Read(bufferOwner.Span);

            collectionIdsByCipherId.TryGetValue(row.CipherId, out var collectionIds);
            attachmentsByCipherId.TryGetValue(row.CipherId, out var attachments);
            var dto = ToDto(row, collectionIds ?? [], attachments ?? []);
            onCipher.Invoke(stateObj, ref dto, bufferOwner.Span[..bytesWritten]);
        }
    }

    private Dictionary<string, CollectionId[]> GetCollectionIdsByCipherId(string userId)
    {
        var rows = Connection.Query<CipherCollectionRow>(
            """
            SELECT
                cipher_id,
                collection_id
            FROM vault_cipher_collection
            WHERE user_id = @UserId
            ORDER BY cipher_id, collection_id;
            """,
            new { UserId = userId },
            transaction: Transaction);

        return rows
            .GroupBy(static row => row.CipherId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static row => CollectionId.Parse(row.CollectionId)).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, VaultCipherAttachmentDownloadResponse[]> GetAttachmentsByCipherId(string userId)
    {
        var rows = Connection.Query<CipherAttachmentRow>(
            """
            SELECT
                cipher_id AS CipherId,
                attachment_id AS AttachmentId,
                encrypted_file_name AS EncryptedFileName,
                size AS Size
            FROM vault_cipher_attachment
            WHERE user_id = @UserId
            ORDER BY cipher_id, sort_order;
            """,
            new { UserId = userId },
            transaction: Transaction);

        return rows
            .GroupBy(static row => row.CipherId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static row => ToDto(row)).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}
