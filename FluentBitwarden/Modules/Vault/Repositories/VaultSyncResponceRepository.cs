using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using Dapper;
using FluentBitwarden.Data;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Modules.Vault.Repositories;

internal sealed class VaultSyncResponceRepository(SqliteTransaction transaction, UserId userId) : BaseRepository(transaction), ISyncDataWriter
{
    private readonly string _userIdStr = userId.ToString();

    public void DeleteVaultData(UserId userId)
    {
        const string sql = """
                           DELETE FROM ciphers     WHERE user_id = @UserId;
                           DELETE FROM collections WHERE user_id = @UserId;
                           DELETE FROM folders     WHERE user_id = @UserId;
                           """;

        Connection.Execute(sql, new { UserId = userId }, transaction: Transaction);
    }

    public void WriteFolder(in FolderDto dto)
    {
        var rowid = Connection.ExecuteScalar<long>(
            """
            INSERT INTO folders (user_id, folder_id, payload)
            VALUES (@UserId, @FolderId, zeroblob(@Size))
            ON CONFLICT (user_id, folder_id) DO UPDATE SET
                payload = zeroblob(@Size)
            RETURNING row_id
            """,
            new
            {
                UserId = _userIdStr,
                FolderId = dto.Id.ToString(),
                Size = dto.Payload.Length
            },
            transaction: Transaction);

        WriteBlob("folders", rowid, dto.Payload);
    }

    public void WriteCollection(in CollectionDto dto)
    {
        var rowid = Connection.ExecuteScalar<long>(
            """
            INSERT INTO collections (user_id, collection_id, payload)
            VALUES (@UserId, @CollectionId, zeroblob(@Size))
            ON CONFLICT (user_id, collection_id) DO UPDATE SET
                payload = zeroblob(@Size)
            RETURNING row_id
            """,
            new
            {
                UserId = _userIdStr,
                CollectionId = dto.Id.ToString(),
                Size = dto.Payload.Length
            },
            transaction: Transaction);

        WriteBlob("collections", rowid, dto.Payload);
    }

    public void WriteCipher(in CipherDto dto)
    {
        var rowid = Connection.ExecuteScalar<long>(
            """
            INSERT INTO ciphers (user_id, cipher_id, folder_id, cipher_type, payload)
            VALUES (@UserId, @CipherId, @FolderId, @CipherType, zeroblob(@Size))
            ON CONFLICT (user_id, cipher_id) DO UPDATE SET
                folder_id   = excluded.folder_id,
                cipher_type = excluded.cipher_type,
                payload     = zeroblob(@Size)
            RETURNING row_id
            """,
            new
            {
                UserId = _userIdStr,
                CipherId = dto.Id.ToString(),
                FolderId = dto.FolderId?.ToString(),
                CipherType = (int)dto.CipherType,
                Size = dto.Payload.Length
            },
            transaction: Transaction);

        WriteBlob("ciphers", rowid, dto.Payload);
    }


    private void WriteBlob(string table, long rowId, ReadOnlySpan<byte> data)
    {
        using var blob = new SqliteBlob(Connection, table, "payload", rowId);
        blob.Write(data);
    }

}
