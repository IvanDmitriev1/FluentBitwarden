using System.Security.Cryptography;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Vault;
using Microsoft.Extensions.Logging;

namespace BitwaredApi.Services;

internal sealed class VaultDataService(
    IApiClient apiClient,
    ICryptoService cryptoService,
    ILogger<VaultDataService> logger)
    : IVaultDataService
{
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

        EncryptedSyncSnapshot snapshot = await VaultSyncResponseParser.CreateSnapshotAsync(
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

    public DecryptedCipher DecryptCipher(CipherSyncItem record, byte[] userKey)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(record.EncryptedPayload);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Encrypted cipher payload root was not an object.");
            }

            byte[]? cipherKey = TryGetEncryptedString(root, "key") is { } wrappedKey
                ? cryptoService.UnwrapKey(new EncString(wrappedKey), userKey)
                : null;

            byte[] decryptionKey = cipherKey ?? userKey;

            try
            {
                string? name = DecryptIfPresent(root, "name", decryptionKey);
                string? notes = DecryptIfPresent(root, "notes", decryptionKey);

                string? username = null;
                string? password = null;
                List<string> uris = [];

                if (TryGetObject(root, "login") is { } login)
                {
                    username = DecryptIfPresent(login, "username", decryptionKey);
                    password = DecryptIfPresent(login, "password", decryptionKey);

                    if (TryGetArray(login, "uris") is { } uriArray)
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
                if (TryGetArray(root, "fields") is { } fieldArray)
                {
                    foreach (JsonElement field in fieldArray.EnumerateArray())
                    {
                        fields.Add(new DecryptedCustomField(
                            DecryptIfPresent(field, "name", decryptionKey),
                            DecryptIfPresent(field, "value", decryptionKey),
                            TryGetOptionalInt32(field, "type")));
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
                    record.RevisionDate);
            }
            finally
            {
                if (cipherKey is not null)
                {
                    cryptoService.ZeroMemory(cipherKey);
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse cached cipher payload for cipher {CipherId} of type {CipherType}.",
                record.Id,
                record.Type);

            return CreateDecryptionErrorCipher(record);
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to decrypt cached cipher payload for cipher {CipherId} of type {CipherType}.",
                record.Id,
                record.Type);

            return CreateDecryptionErrorCipher(record);
        }
    }

    public IReadOnlyList<DecryptedCipher> DecryptCiphers(IReadOnlyList<CipherSyncItem> records, byte[] userKey)
    {
        return [.. records.Select(record => DecryptCipher(record, userKey))];
    }

    private DecryptedCipher CreateDecryptionErrorCipher(CipherSyncItem record)
        => new(
            record.Id,
            (CipherType)record.Type,
            null,
            null,
            null,
            null,
            [],
            [],
            record.FolderId,
            record.OrganizationId,
            record.RevisionDate,
            true);

    private string? DecryptIfPresent(JsonElement element, string propertyName, byte[] key)
    {
        string? value = TryGetEncryptedString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return cryptoService.DecryptString(new EncString(value), key);
    }

    private static string? TryGetEncryptedString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static JsonElement? TryGetObject(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return property;
    }

    private static JsonElement? TryGetArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return property;
    }

    private static int? TryGetOptionalInt32(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out int value))
        {
            return null;
        }

        return value;
    }
}
