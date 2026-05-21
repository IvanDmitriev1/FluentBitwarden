using BitwardenApi.Models;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Vault.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Modules.Vault.Repositories;

internal sealed class VaultWriterRepository(SqliteTransaction transaction, UserId userId)
    : BaseRepository(transaction), IVaultWriterRepository
{
    private readonly string _userIdStr = userId.ToString();

    public void WriteFolders(ReadOnlySpan<VaultFolderDto> folders)
    {
        if (folders.Length == 0)
            return;

        foreach (ref readonly var dto in folders)
        {
            Connection.Execute(
                """
                INSERT INTO folders (user_id, folder_id, revision_date_unix_ms, encrypted_name)
                VALUES (@UserId, @FolderId, @RevisionDateUnixMs, @EncryptedName)
                ON CONFLICT (user_id, folder_id) DO UPDATE SET
                    revision_date_unix_ms = excluded.revision_date_unix_ms,
                    encrypted_name = excluded.encrypted_name
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
        if (collections.Length == 0)
            return;
        foreach (ref readonly var dto in collections)
        {
            Connection.Execute(
                """
                INSERT INTO collections (
                    user_id,
                    collection_id,
                    organization_id,
                    read_only,
                    manage,
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
                ON CONFLICT (user_id, collection_id) DO UPDATE SET
                    organization_id = excluded.organization_id,
                    read_only = excluded.read_only,
                    manage = excluded.manage,
                    hide_passwords = excluded.hide_passwords,
                    collection_type = excluded.collection_type,
                    encrypted_name = excluded.encrypted_name
                """,
                new
                {
                    UserId = _userIdStr,
                    CollectionId = dto.Id.ToString(),
                    OrganizationId = dto.OrganizationId?.ToString(),
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
        if (ciphers.Length == 0)
            return;

        foreach (ref readonly var dto in ciphers)
        {
            var rowId = Connection.ExecuteScalar<long>(
                """
                INSERT INTO ciphers (
                    user_id,
                    cipher_id,
                    organization_id,
                    folder_id,
                    cipher_type,
                    revision_date_unix_ms,
                    creation_date_unix_ms,
                    deleted_date_unix_ms,
                    archived_date_unix_ms,
                    favorite,
                    reprompt,
                    edit,
                    view_password,
                    encrypted_key,
                    payload)
                VALUES (
                    @UserId,
                    @CipherId,
                    @OrganizationId,
                    @FolderId,
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
                ON CONFLICT (user_id, cipher_id) DO UPDATE SET
                    organization_id = excluded.organization_id,
                    folder_id = excluded.folder_id,
                    cipher_type = excluded.cipher_type,
                    revision_date_unix_ms = excluded.revision_date_unix_ms,
                    creation_date_unix_ms = excluded.creation_date_unix_ms,
                    deleted_date_unix_ms = excluded.deleted_date_unix_ms,
                    archived_date_unix_ms = excluded.archived_date_unix_ms,
                    favorite = excluded.favorite,
                    reprompt = excluded.reprompt,
                    edit = excluded.edit,
                    view_password = excluded.view_password,
                    encrypted_key = excluded.encrypted_key,
                    payload = zeroblob(@Size)
                RETURNING row_id
                """,
                new
                {
                    UserId = _userIdStr,
                    CipherId = dto.Id.ToString(),
                    OrganizationId = dto.OrganizationId?.ToString(),
                    FolderId = dto.FolderId?.ToString(),
                    CipherType = (int)dto.CipherType,
                    RevisionDateUnixMs = dto.RevisionDate.ToUnixTimeMilliseconds(),
                    CreationDateUnixMs = dto.CreationDate.ToUnixTimeMilliseconds(),
                    DeletedDateUnixMs = dto.DeletedDate?.ToUnixTimeMilliseconds(),
                    ArchivedDateUnixMs = dto.ArchivedDate?.ToUnixTimeMilliseconds(),
                    Favorite = dto.Favorite ? 1 : 0,
                    Reprompt = dto.Reprompt ? 1 : 0,
                    Edit = dto.Edit ? 1 : 0,
                    ViewPassword = dto.ViewPassword ? 1 : 0,
                    EncryptedKey = dto.EncryptedKey?.ToByteArray(),
                    Size = dto.Data.Length
                },
                transaction: Transaction);

            WriteBlob("ciphers", rowId, dto.Data);
        }
    }

    public void DeleteVaultData()
    {
        const string sql = """
                           DELETE FROM ciphers     WHERE user_id = @UserId;
                           DELETE FROM collections WHERE user_id = @UserId;
                           DELETE FROM folders     WHERE user_id = @UserId;
                           """;

        Connection.Execute(sql, new { UserId = _userIdStr }, transaction: Transaction);
    }

    private void WriteBlob(string table, long rowId, ReadOnlySpan<byte> data)
    {
        using var blob = new SqliteBlob(Connection, table, "payload", rowId);
        blob.Write(data);
    }
}
