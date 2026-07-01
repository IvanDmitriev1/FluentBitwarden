using Dapper;
using FluentBitwarden.AppHost.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;

internal sealed class VaultWriterRepository(SqliteTransaction transaction, UserId userId) : BaseRepository(transaction)
{
    private readonly string _userIdStr = userId.ToString();

    public void WriteOrganizations(ReadOnlySpan<VaultOrganizationDto> organizations)
    {
        Connection.Execute("DELETE FROM vault_organization WHERE user_id = @UserId;", new { UserId = _userIdStr }, transaction: Transaction);

        foreach (ref readonly var dto in organizations)
        {
            Connection.Execute(
                """
                INSERT INTO vault_organization (
                    user_id,
                    organization_id,
                    organization_user_id,
                    organization_name,
                    is_enabled,
                    access_secrets_manager,
                    member_status,
                    encrypted_organization_key)
                VALUES (
                    @UserId,
                    @OrganizationId,
                    @OrganizationUserId,
                    @OrganizationName,
                    @IsEnabled,
                    @AccessSecretsManager,
                    @MemberStatus,
                    @EncryptedOrganizationKey)
                """,
                new
                {
                    UserId = _userIdStr,
                    OrganizationId = dto.Id.ToString(),
                    OrganizationUserId = dto.OrganizationUserId == Guid.Empty
                        ? null
                        : dto.OrganizationUserId.ToString(),
                    OrganizationName = dto.Name,
                    IsEnabled = dto.Enabled ? 1 : 0,
                    AccessSecretsManager = dto.AccessSecretsManager ? 1 : 0,
                    MemberStatus = dto.Status,
                    EncryptedOrganizationKey = dto.EncryptedOrganizationKey.IsEmpty
                        ? null
                        : dto.EncryptedOrganizationKey.ToByteArray()
                },
                transaction: Transaction);
        }
    }

    public void WriteFolders(ReadOnlySpan<VaultFolderDto> folders)
    {
        Connection.Execute("DELETE FROM vault_folder WHERE user_id = @UserId;", new { UserId = _userIdStr }, transaction: Transaction);

        foreach (ref readonly var dto in folders)
        {
            Connection.Execute(
                """
                INSERT INTO vault_folder (user_id, folder_id, revision_date_unix_ms, encrypted_name)
                VALUES (@UserId, @FolderId, @RevisionDateUnixMs, @EncryptedName)
                """,
                new
                {
                    UserId = _userIdStr,
                    FolderId = dto.Id.ToString(),
                    RevisionDateUnixMs = dto.RevisionDate.ToUnixTimeMilliseconds(),
                    EncryptedName = dto.EncryptedName.ToByteArray()
                },
                transaction: Transaction);
        }
    }

    public void WriteCollections(ReadOnlySpan<VaultCollectionDto> collections)
    {
        Connection.Execute("DELETE FROM vault_collection WHERE user_id = @UserId;", new { UserId = _userIdStr }, transaction: Transaction);

        foreach (ref readonly var dto in collections)
        {
            Connection.Execute(
                """
                INSERT INTO vault_collection (
                    user_id,
                    collection_id,
                    organization_id,
                    is_read_only,
                    can_manage,
                    hide_passwords,
                    collection_type,
                    encrypted_name)
                VALUES (
                    @UserId,
                    @CollectionId,
                    @OrganizationId,
                    @ReadOnly,
                    @Manage,
                    @HidePasswords,
                    @CollectionType,
                    @EncryptedName)
                """,
                new
                {
                    UserId = _userIdStr,
                    CollectionId = dto.Id.ToString(),
                    OrganizationId = dto.OrganizationId.IsEmpty
                        ? null
                        : dto.OrganizationId.ToString(),
                    ReadOnly = dto.ReadOnly ? 1 : 0,
                    Manage = dto.Manage ? 1 : 0,
                    HidePasswords = dto.HidePasswords ? 1 : 0,
                    CollectionType = dto.Type,
                    EncryptedName = dto.EncryptedName.ToByteArray()
                },
                transaction: Transaction);
        }
    }

    public void WriteCiphers(ReadOnlySpan<VaultCipherDto> ciphers)
    {
        Connection.Execute("DELETE FROM vault_cipher WHERE user_id = @UserId;", new { UserId = _userIdStr }, transaction: Transaction);

        foreach (ref readonly var dto in ciphers)
        {
            var cipherId = dto.Id.ToString();
            var rowId = Connection.ExecuteScalar<long>(
                """
                INSERT INTO vault_cipher (
                    user_id,
                    cipher_id,
                    organization_id,
                    cipher_type,
                    revision_date_unix_ms,
                    creation_date_unix_ms,
                    deleted_date_unix_ms,
                    archived_date_unix_ms,
                    is_favorite,
                    reprompt,
                    can_edit,
                    can_view_password,
                    encrypted_cipher_key,
                    encrypted_payload)
                VALUES (
                    @UserId,
                    @CipherId,
                    @OrganizationId,
                    @CipherType,
                    @RevisionDateUnixMs,
                    @CreationDateUnixMs,
                    @DeletedDateUnixMs,
                    @ArchivedDateUnixMs,
                    @Favorite,
                    @Reprompt,
                    @Edit,
                    @ViewPassword,
                    @EncryptedKey,
                    zeroblob(@Size))
                RETURNING row_id
                """,
                new
                {
                    UserId = _userIdStr,
                    CipherId = cipherId,
                    OrganizationId = dto.OrganizationId.IsEmpty
                        ? null
                        : dto.OrganizationId.ToString(),
                    CipherType = (int)dto.VaultCipherType,
                    RevisionDateUnixMs = dto.RevisionDate.ToUnixTimeMilliseconds(),
                    CreationDateUnixMs = dto.CreationDate.ToUnixTimeMilliseconds(),
                    DeletedDateUnixMs = dto.DeletedDate?.ToUnixTimeMilliseconds(),
                    ArchivedDateUnixMs = dto.ArchivedDate?.ToUnixTimeMilliseconds(),
                    Favorite = dto.Favorite ? 1 : 0,
                    Reprompt = dto.Reprompt ? 1 : 0,
                    Edit = dto.Edit ? 1 : 0,
                    ViewPassword = dto.ViewPassword ? 1 : 0,
                    EncryptedKey = dto.EncryptedKey.IsEmpty
                        ? null
                        : dto.EncryptedKey.ToByteArray(),
                    Size = dto.Data.Length
                },
                transaction: Transaction);

            WritePayloadBlob(rowId, dto.Data);
            WriteCipherAssignments(cipherId, dto.FolderId, dto.CollectionIds);
        }
    }

    private void WriteCipherAssignments(string cipherId, FolderId folderId, CollectionId[] collectionIds)
    {
        Connection.Execute(
            """
            DELETE FROM vault_cipher_collection WHERE user_id = @UserId AND cipher_id = @CipherId;
            DELETE FROM vault_cipher_folder WHERE user_id = @UserId AND cipher_id = @CipherId;
            """,
            new { UserId = _userIdStr, CipherId = cipherId },
            transaction: Transaction);

        if (!folderId.IsEmpty)
        {
            Connection.Execute(
                """
                INSERT INTO vault_cipher_folder (user_id, cipher_id, folder_id)
                VALUES (@UserId, @CipherId, @FolderId)
                """,
                new
                {
                    UserId = _userIdStr,
                    CipherId = cipherId,
                    FolderId = folderId.ToString()
                },
                transaction: Transaction);
        }

        if (collectionIds.Length == 0)
            return;

        foreach (var collectionId in collectionIds)
        {
            if (collectionId.IsEmpty)
                continue;

            Connection.Execute(
                """
                INSERT OR IGNORE INTO vault_cipher_collection (user_id, cipher_id, collection_id)
                VALUES (@UserId, @CipherId, @CollectionId)
                """,
                new
                {
                    UserId = _userIdStr,
                    CipherId = cipherId,
                    CollectionId = collectionId.ToString()
                },
                transaction: Transaction);
        }
    }

    private void WritePayloadBlob(long rowId, ReadOnlySpan<byte> data)
    {
        using var blob = new SqliteBlob(Connection, "vault_cipher", "encrypted_payload", rowId);
        blob.Write(data);
    }
}
