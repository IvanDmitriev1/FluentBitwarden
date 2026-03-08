using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Crypto.Enc;
using BitwaredApi.Http;
using BitwaredApi.Models.Session;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Services;

public sealed class VaultService : IVaultService
{
    private readonly ApiClient _apiClient;
    private readonly IVaultCache _vaultCache;
    private readonly SessionCoordinator _sessionCoordinator;
    private readonly ICryptoService _cryptoService;
    private readonly IClock _clock;

    public VaultService(
        ApiClient apiClient,
        IVaultCache vaultCache,
        SessionCoordinator sessionCoordinator,
        ICryptoService cryptoService,
        IClock clock)
    {
        _apiClient = apiClient;
        _vaultCache = vaultCache;
        _sessionCoordinator = sessionCoordinator;
        _cryptoService = cryptoService;
        _clock = clock;
    }

    public async ValueTask<SyncSummary> SyncAsync(CancellationToken cancellationToken = default)
    {
        SessionState state = await _sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No active Bitwarden session is available.");

        await _vaultCache.InitializeAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument syncDocument = await _apiClient.GetSyncAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset lastSyncUtc = _clock.UtcNow;
        DateTimeOffset? revisionDate = await _apiClient.GetRevisionDateAsync(cancellationToken).ConfigureAwait(false);

        List<EncryptedCipherRecord> ciphers = ExtractCiphers(syncDocument.RootElement, state.AccountId, lastSyncUtc);
        List<EncryptedFolderRecord> folders = ExtractFolders(syncDocument.RootElement, state.AccountId, lastSyncUtc);
        List<EncryptedCollectionRecord> collections = ExtractCollections(syncDocument.RootElement, state.AccountId, lastSyncUtc);

        EncryptedSyncSnapshot snapshot = new(
            new VaultAccountRecord(
                state.AccountId,
                state.Email,
                state.ApiBase,
                state.IdentityBase,
                lastSyncUtc,
                lastSyncUtc),
            new VaultSyncStateRecord(state.AccountId, revisionDate, lastSyncUtc),
            ciphers,
            folders,
            collections);

        await _vaultCache.SaveSyncAsync(snapshot, cancellationToken).ConfigureAwait(false);

        return new SyncSummary(ciphers.Count, folders.Count, collections.Count, revisionDate, lastSyncUtc);
    }

    public async ValueTask<IReadOnlyList<DecryptedCipher>> ListCiphersAsync(CancellationToken cancellationToken = default)
    {
        SessionState state = await _sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No active Bitwarden session is available.");

        byte[] userKey = _sessionCoordinator.GetUserKeyCopy()
            ?? throw new InvalidOperationException("The vault is locked. Sign in again to decrypt cached items.");

        try
        {
            IReadOnlyList<EncryptedCipherRecord> records = await _vaultCache.ListCiphersAsync(state.AccountId, cancellationToken).ConfigureAwait(false);
            return records.Select(record => DecryptCipher(record, userKey)).ToArray();
        }
        finally
        {
            _cryptoService.ZeroMemory(userKey);
        }
    }

    public async ValueTask<DecryptedCipher?> GetCipherAsync(string id, CancellationToken cancellationToken = default)
    {
        SessionState state = await _sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No active Bitwarden session is available.");

        byte[] userKey = _sessionCoordinator.GetUserKeyCopy()
            ?? throw new InvalidOperationException("The vault is locked. Sign in again to decrypt cached items.");

        try
        {
            EncryptedCipherRecord? record = await _vaultCache.GetCipherAsync(state.AccountId, id, cancellationToken).ConfigureAwait(false);
            return record is null ? null : DecryptCipher(record, userKey);
        }
        finally
        {
            _cryptoService.ZeroMemory(userKey);
        }
    }

    private DecryptedCipher DecryptCipher(EncryptedCipherRecord record, byte[] userKey)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(record.EncryptedJson);
            JsonElement root = document.RootElement;

            byte[]? cipherKey = root.TryGetProperty("key", out JsonElement keyProp)
                && keyProp.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(keyProp.GetString())
                ? _cryptoService.UnwrapKey(new Models.Vault.EncString(keyProp.GetString()!), userKey)
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
                    _cryptoService.ZeroMemory(cipherKey);
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

        return _cryptoService.DecryptString(new Models.Vault.EncString(value), key);
    }

    private static List<EncryptedCipherRecord> ExtractCiphers(JsonElement root, string accountId, DateTimeOffset updatedUtc)
    {
        List<EncryptedCipherRecord> records = [];

        if (!root.TryGetProperty("ciphers", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            return records;
        }

        foreach (JsonElement item in array.EnumerateArray())
        {
            records.Add(new EncryptedCipherRecord(
                accountId,
                item.GetProperty("id").GetString() ?? throw new InvalidOperationException("Cipher payload is missing Id."),
                item.GetProperty("type").GetInt32(),
                item.TryGetProperty("organizationId", out JsonElement orgId) ? orgId.GetString() : null,
                item.TryGetProperty("folderId", out JsonElement folderId) ? folderId.GetString() : null,
                item.TryGetProperty("collectionIds", out JsonElement collectionIds) ? collectionIds.GetRawText() : "[]",
                item.TryGetProperty("revisionDate", out JsonElement revisionDate) && DateTimeOffset.TryParse(revisionDate.GetString(), out DateTimeOffset parsedRevisionDate)
                    ? parsedRevisionDate
                    : null,
                item.GetRawText(),
                updatedUtc));
        }

        return records;
    }

    private static List<EncryptedFolderRecord> ExtractFolders(JsonElement root, string accountId, DateTimeOffset updatedUtc)
    {
        List<EncryptedFolderRecord> records = [];

        if (!root.TryGetProperty("folders", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            return records;
        }

        foreach (JsonElement item in array.EnumerateArray())
        {
            records.Add(new EncryptedFolderRecord(
                accountId,
                item.GetProperty("id").GetString() ?? throw new InvalidOperationException("Folder payload is missing Id."),
                item.TryGetProperty("revisionDate", out JsonElement revisionDate) && DateTimeOffset.TryParse(revisionDate.GetString(), out DateTimeOffset parsedRevisionDate)
                    ? parsedRevisionDate
                    : null,
                item.GetRawText(),
                updatedUtc));
        }

        return records;
    }

    private static List<EncryptedCollectionRecord> ExtractCollections(JsonElement root, string accountId, DateTimeOffset updatedUtc)
    {
        List<EncryptedCollectionRecord> records = [];

        if (!root.TryGetProperty("collections", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            return records;
        }

        foreach (JsonElement item in array.EnumerateArray())
        {
            records.Add(new EncryptedCollectionRecord(
                accountId,
                item.GetProperty("id").GetString() ?? throw new InvalidOperationException("Collection payload is missing Id."),
                item.TryGetProperty("revisionDate", out JsonElement revisionDate) && DateTimeOffset.TryParse(revisionDate.GetString(), out DateTimeOffset parsedRevisionDate)
                    ? parsedRevisionDate
                    : null,
                item.GetRawText(),
                updatedUtc));
        }

        return records;
    }
}
