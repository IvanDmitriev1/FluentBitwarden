using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Services;

internal sealed class VaultDataService(
    IApiClient apiClient,
    ICryptoService cryptoService)
    : IVaultDataService
{
    private const int InitialBufferSize = 64 * 1024;

    public async ValueTask<VaultSyncResult> SyncAsync(
        VaultSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset? remoteRevisionDate = await apiClient.GetRevisionDateAsync(
            request.Environment,
            request.AccessToken,
            cancellationToken);

        if (request.HasCachedData
            && remoteRevisionDate is not null
            && request.CachedRevisionDate == remoteRevisionDate)
        {
            return new VaultSyncResult.NotModified(
                new SyncSummary(
                    request.CachedCipherCount,
                    request.CachedFolderCount,
                    request.CachedCollectionCount,
                    request.CachedRevisionDate,
                    request.LastSyncUtc ?? DateTimeOffset.UtcNow));
        }

        using var response = await apiClient.CreateSyncResponseAsync(
            request.Environment,
            request.AccessToken,
            cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        DateTimeOffset lastSyncUtc = DateTimeOffset.UtcNow;

        EncryptedSyncSnapshot snapshot = await CreateSnapshotAsync(
            stream,
            request,
            lastSyncUtc,
            remoteRevisionDate,
            cancellationToken);

        return new VaultSyncResult.Updated(
            snapshot,
            new SyncSummary(
                snapshot.SyncState.CipherCount,
                snapshot.SyncState.FolderCount,
                snapshot.SyncState.CollectionCount,
                snapshot.SyncState.RevisionDate,
                snapshot.SyncState.LastSyncUtc));
    }

    public DecryptedCipher DecryptCipher(EncryptedCipherRecord record, byte[] userKey)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(record.EncryptedPayload);
            JsonElement root = document.RootElement;

            byte[]? cipherKey = root.TryGetProperty("key", out JsonElement keyProp)
                && keyProp.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(keyProp.GetString())
                ? cryptoService.UnwrapKey(new EncString(keyProp.GetString()!), userKey)
                : null;

            byte[] decryptionKey = cipherKey ?? userKey;

            try
            {
                string? name = DecryptIfPresent(root, "name", decryptionKey);
                string? notes = DecryptIfPresent(root, "notes", decryptionKey);

                string? username = null;
                string? password = null;
                List<string> uris = [];

                if (root.TryGetProperty("login", out JsonElement login) && login.ValueKind == JsonValueKind.Object)
                {
                    username = DecryptIfPresent(login, "username", decryptionKey);
                    password = DecryptIfPresent(login, "password", decryptionKey);

                    if (login.TryGetProperty("uris", out JsonElement uriArray) && uriArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement uri in uriArray.EnumerateArray())
                        {
                            string? value = DecryptIfPresent(uri, "uri", decryptionKey);
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                uris.Add(value);
                            }
                        }
                    }
                }

                List<DecryptedCustomField> fields = [];
                if (root.TryGetProperty("fields", out JsonElement fieldArray) && fieldArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement field in fieldArray.EnumerateArray())
                    {
                        fields.Add(new DecryptedCustomField(
                            DecryptIfPresent(field, "name", decryptionKey),
                            DecryptIfPresent(field, "value", decryptionKey),
                            field.TryGetProperty("type", out JsonElement typeProp) && typeProp.ValueKind == JsonValueKind.Number
                                ? typeProp.GetInt32()
                                : null));
                    }
                }

                return new DecryptedCipher(
                    record.Id,
                    (CipherType)record.Type,
                    name,
                    username,
                    password,
                    notes,
                    uris,
                    fields,
                    record.FolderId,
                    record.OrganizationId,
                    record.RevisionDate,
                    false);
            }
            finally
            {
                if (cipherKey is not null)
                {
                    cryptoService.ZeroMemory(cipherKey);
                }
            }
        }
        catch
        {
            return new DecryptedCipher(
                record.Id,
                (CipherType)record.Type,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<DecryptedCustomField>(),
                record.FolderId,
                record.OrganizationId,
                record.RevisionDate,
                true);
        }
    }

    public IReadOnlyList<DecryptedCipher> DecryptCiphers(IReadOnlyList<EncryptedCipherRecord> records, byte[] userKey)
    {
        return [.. records.Select(record => DecryptCipher(record, userKey))];
    }

    private static async ValueTask<EncryptedSyncSnapshot> CreateSnapshotAsync(
        Stream stream,
        VaultSyncRequest request,
        DateTimeOffset lastSyncUtc,
        DateTimeOffset? revisionDate,
        CancellationToken cancellationToken)
    {
        List<EncryptedCipherRecord> ciphers = [];
        List<EncryptedFolderRecord> folders = [];
        List<EncryptedCollectionRecord> collections = [];

        using ArrayPoolBufferWriter<byte> bufferWriter = new(InitialBufferSize);
        int bytesInBuffer = 0;
        JsonReaderState readerState = default;
        bool isFinalBlock = false;
        SyncSection? currentSection = null;

        while (!isFinalBlock || bytesInBuffer > 0)
        {
            if (!isFinalBlock && bytesInBuffer < bufferWriter.Capacity / 2)
            {
                Memory<byte> readBuffer = bufferWriter.GetMemory(1);
                int read = await stream.ReadAsync(readBuffer, cancellationToken);
                if (read == 0)
                {
                    isFinalBlock = true;
                }
                else
                {
                    bufferWriter.Advance(read);
                    bytesInBuffer += read;
                }
            }

            Utf8JsonReader reader = new(bufferWriter.WrittenSpan, isFinalBlock, readerState);
            bool needsMoreData = false;

            while (reader.Read())
            {
                if (currentSection is null)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
                    {
                        continue;
                    }

                    string propertyName = reader.GetString()
                        ?? throw new ServerVersionMismatchException("Sync response property name was missing.");

                    if (!reader.Read())
                    {
                        needsMoreData = true;
                        break;
                    }

                    currentSection = propertyName switch
                    {
                        "ciphers" when reader.TokenType == JsonTokenType.StartArray => SyncSection.Ciphers,
                        "folders" when reader.TokenType == JsonTokenType.StartArray => SyncSection.Folders,
                        "collections" when reader.TokenType == JsonTokenType.StartArray => SyncSection.Collections,
                        _ => null,
                    };

                    if (currentSection is null)
                    {
                        if (!reader.TrySkip())
                        {
                            needsMoreData = true;
                            break;
                        }
                    }

                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 1)
                {
                    currentSection = null;
                    continue;
                }

                if (reader.TokenType != JsonTokenType.StartObject || reader.CurrentDepth != 2)
                {
                    continue;
                }

                if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? itemDocument))
                {
                    needsMoreData = true;
                    break;
                }

                using var _ = itemDocument ??
                              throw new ServerVersionMismatchException("Sync item payload was incomplete.");

                switch (currentSection.Value)
                {
                    case SyncSection.Ciphers:
                        ciphers.Add(VaultSyncJsonMapper.MapCipher(itemDocument.RootElement, request.AccountId, lastSyncUtc));
                        break;
                    case SyncSection.Folders:
                        folders.Add(VaultSyncJsonMapper.MapFolder(itemDocument.RootElement, request.AccountId, lastSyncUtc));
                        break;
                    case SyncSection.Collections:
                        collections.Add(VaultSyncJsonMapper.MapCollection(itemDocument.RootElement, request.AccountId, lastSyncUtc));
                        break;
                    default:
                        throw new ServerVersionMismatchException("Sync response contained an unsupported section.");
                }
            }

            int consumed = checked((int)reader.BytesConsumed);
            bytesInBuffer -= consumed;
            bufferWriter.CompactUnreadBytes(consumed, bytesInBuffer);
            readerState = reader.CurrentState;

            if (!needsMoreData)
            {
                continue;
            }

            if (isFinalBlock)
            {
                throw new ServerVersionMismatchException("Sync response ended unexpectedly.");
            }

            if (bytesInBuffer == bufferWriter.Capacity)
            {
                _ = bufferWriter.GetSpan(1);
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

    private string? DecryptIfPresent(JsonElement element, string propertyName, byte[] key)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string? value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return cryptoService.DecryptString(new EncString(value), key);
    }

    private enum SyncSection
    {
        Ciphers,
        Folders,
        Collections,
    }
}
