using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Modules.Vault.VaultDataParser;
using CommunityToolkit.HighPerformance.Buffers;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Vault.Abstractions;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace FluentBitwarden.Modules.Vault.Services;

internal sealed class CipherRepository(SqliteTransaction transaction) : BaseRepository(transaction), ICipherRepository
{
    private readonly record struct CipherRow(
        int RowId,
        string CipherId,
        string? OrganizationId,
        string? FolderId,
        string? EncryptedKey,
        int CipherType,
        long RevisionDateUnixMs,
        long CreationDateUnixMs,
        long? DeletedDateUnixMs,
        long? ArchivedDateUnixMs,
        int Favorite,
        int Reprompt,
        int Edit,
        int ViewPassword);

    public List<Cipher> GetCiphers(DecryptedUserKey decryptedUserKey)
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
            new { UserId = decryptedUserKey.UserId.ToString() },
            transaction: Transaction);

        var cipherList = new List<Cipher>();

        foreach (var cipherRow in cipherRows)
        {
            using var payloadOwner = GetPayload(cipherRow.RowId);

            try
            {
                var dto = new CipherDto
                {
                    Id = CipherId.Parse(cipherRow.CipherId),
                    OrganizationId = cipherRow.OrganizationId is null ? null : OrganizationId.Parse(cipherRow.OrganizationId),
                    FolderId = cipherRow.FolderId is null ? null : FolderId.Parse(cipherRow.FolderId),
                    EncryptedKey = cipherRow.EncryptedKey,
                    CipherType = (CipherType)cipherRow.CipherType,
                    RevisionDate = DateTimeOffset.FromUnixTimeMilliseconds(cipherRow.RevisionDateUnixMs),
                    CreationDate = DateTimeOffset.FromUnixTimeMilliseconds(cipherRow.CreationDateUnixMs),
                    DeletedDate = cipherRow.DeletedDateUnixMs is { } deletedDateUnixMs
                        ? DateTimeOffset.FromUnixTimeMilliseconds(deletedDateUnixMs)
                        : null,
                    ArchivedDate = cipherRow.ArchivedDateUnixMs is { } archivedDateUnixMs
                        ? DateTimeOffset.FromUnixTimeMilliseconds(archivedDateUnixMs)
                        : null,
                    Favorite = cipherRow.Favorite != 0,
                    Reprompt = cipherRow.Reprompt != 0,
                    Edit = cipherRow.Edit != 0,
                    ViewPassword = cipherRow.ViewPassword != 0
                };

                cipherList.Add(VaultDataParser.ParseAndDecryptCipher(dto, payloadOwner.Span, decryptedUserKey));
            }
            catch (Exception e)
            {
                Trace.TraceWarning(
                    "Skipping cipher {0} because it could not be parsed or decrypted: {1}",
                    cipherRow.CipherId,
                    e);
            }
        }

        return cipherList;
    }

    public Cipher GetCipher(CipherId cipherId, DecryptedUserKey decryptedUserKey)
    {
        throw new NotImplementedException();
    }

    private MemoryOwner<byte> GetPayload(long rowId)
    {
        using var blob = new SqliteBlob(Connection, "ciphers", "payload", rowId, readOnly: true);
        var buffer = MemoryOwner<byte>.Allocate((int)blob.Length);

        _ = blob.Read(buffer.Span);
        return buffer;
    }
}
