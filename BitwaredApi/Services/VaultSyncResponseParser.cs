using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Services;

internal static class VaultSyncResponseParser
{
    public static async ValueTask<EncryptedSyncSnapshot> CreateSnapshotAsync(
        Stream stream,
        VaultSyncRequest request,
        DateTimeOffset lastSyncUtc,
        DateTimeOffset? revisionDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var document = await JsonDocument
                .ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new ServerVersionMismatchException("Sync response root was not a JSON object.");
            }

            List<CipherSyncItem> ciphers = [];
            List<FolderSyncItem> folders = [];
            List<CollectionSyncItem> collections = [];

            foreach (JsonProperty property in root.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "ciphers":
                        property.Value.AddItemsTo("ciphers", ciphers, static item => item.ToCipherSyncItem());
                        break;
                    case "folders":
                        property.Value.AddItemsTo("folders", folders, static item => item.ToFolderSyncItem());
                        break;
                    case "collections":
                        property.Value.AddItemsTo("collections", collections, static item => item.ToCollectionSyncItem());
                        break;
                }
            }

            return new EncryptedSyncSnapshot(
                new VaultAccountRecord(
                    request.AccountId,
                    request.Email,
                    request.Environment.ApiBase.ToString(),
                    request.Environment.IdentityBase.ToString(),
                    lastSyncUtc,
                    lastSyncUtc),
                new VaultSyncStateRecord(
                    request.AccountId,
                    revisionDate,
                    lastSyncUtc,
                    ciphers.Count,
                    folders.Count,
                    collections.Count),
                ciphers,
                folders,
                collections);
        }
        catch (JsonException ex)
        {
            throw new ServerVersionMismatchException("Sync response payload was not a supported JSON object.", ex);
        }
    }
}
