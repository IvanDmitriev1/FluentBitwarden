using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Modules.Vault.VaultDataParser;
using CommunityToolkit.HighPerformance.Buffers;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Vault.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Modules.Vault.Services;

internal sealed class CipherRepository(SqliteTransaction transaction) : BaseRepository(transaction), ICipherRepository
{
    private readonly record struct CipherRow(
        int RowId,
        string UserId,
        string CipherId,
        string? FolderId,
        int CipherType);

    public List<Cipher> GetCiphers(DecryptedUserKey decryptedUserKey)
    {
        var ciphers = Connection.Query<CipherRow>("""
                                                 SELECT
                                                    row_id,
                                                    user_id,
                                                    cipher_id,
                                                    folder_id,
                                                    cipher_type
                                                 FROM ciphers
                                                 WHERE user_id = @UserId
                                                 ORDER BY row_id;
                                                 """, new { UserId = decryptedUserKey.UserId.ToString() }, transaction: Transaction);


        var cipherList = new List<Cipher>();

        foreach (var cipherRow in ciphers)
        {
            using var payloadOwner = GetPayload(cipherRow.RowId);

            try
            {
                cipherList.Add(VaultDataParser.ParseAndDecryptCipher(new CipherDto()
                {
                    Id = CipherId.Parse(cipherRow.CipherId),
                    FolderId = cipherRow.FolderId is null ? null : FolderId.Parse(cipherRow.FolderId),
                    CipherType = (CipherType)cipherRow.CipherType,
                    Payload = payloadOwner.Span
                }, decryptedUserKey));

            }
            catch (Exception e)
            {
                //
            }

            
        }

        return cipherList;
    }

    public Cipher GetCipher(CipherId cipherId, DecryptedUserKey decryptedUserKey)
    {
        throw new NotImplementedException();
    }

    private MemoryOwner<byte> GetPayload(Int64 rowId)
    {
        using var blob = new SqliteBlob(Connection, "ciphers", "payload", rowId, readOnly: true);
        var buffer = MemoryOwner<byte>.Allocate((int)blob.Length);

        int read = blob.Read(buffer.Span);
        return buffer;
    }
}