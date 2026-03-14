using System.Security.Cryptography;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Vault;
using Microsoft.Extensions.Logging;

namespace BitwaredApi.Services;

internal sealed class VaultDataService(
    IApiClient apiClient,
    ICryptoService cryptoService,
    ILogger<VaultDataService> logger)
    : IVaultDataService
{
    private const string CachedVaultDecryptionFailedMessage = "Cached vault data could not be decrypted.";

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

    public VaultDecryptionOutcome<DecryptedCipher> DecryptCipher(CipherSyncItem record, byte[] userKey)
    {
        try
        {
            return new VaultDecryptionOutcome<DecryptedCipher>.Success(
                DecryptCipherCore(record, userKey));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse cached cipher payload for cipher {CipherId} of type {CipherType}.",
                record.Id,
                record.Type);

            return new VaultDecryptionOutcome<DecryptedCipher>.Failed(CachedVaultDecryptionFailedMessage);
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to decrypt cached cipher payload for cipher {CipherId} of type {CipherType}.",
                record.Id,
                record.Type);

            return new VaultDecryptionOutcome<DecryptedCipher>.Failed(CachedVaultDecryptionFailedMessage);
        }
    }

    public VaultDecryptionOutcome<IReadOnlyList<DecryptedCipher>> DecryptCiphers(
        IReadOnlyList<CipherSyncItem> records,
        byte[] userKey)
    {
        List<DecryptedCipher> decrypted = new(records.Count);

        foreach (CipherSyncItem record in records)
        {
            VaultDecryptionOutcome<DecryptedCipher> outcome = DecryptCipher(record, userKey);
            switch (outcome)
            {
                case VaultDecryptionOutcome<DecryptedCipher>.Success success:
                    decrypted.Add(success.Value);
                    break;

                case VaultDecryptionOutcome<DecryptedCipher>.Failed failed:
                    return new VaultDecryptionOutcome<IReadOnlyList<DecryptedCipher>>.Failed(failed.Message);

                default:
                    throw new InvalidOperationException("Unsupported vault decryption outcome.");
            }
        }

        return new VaultDecryptionOutcome<IReadOnlyList<DecryptedCipher>>.Success(decrypted);
    }

    private CipherContent ReadCipherContent(byte[] payload, ReadOnlySpan<byte> key)
    {
        Utf8JsonReader reader = new(payload, isFinalBlock: true, state: default);
        reader.ReadRequiredStartObject("Encrypted cipher payload root was not an object.");

        string? name = null;
        string? notes = null;
        string? username = null;
        string? password = null;
        List<string> uris = [];
        List<DecryptedCustomField> fields = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                reader.EnsureNoTrailingData();
                return new CipherContent(name, username, password, notes, uris, fields);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Encrypted cipher payload contained an unexpected token.");
            }

            bool isKey = reader.ValueTextEquals("key");
            bool isName = reader.ValueTextEquals("name");
            bool isNotes = reader.ValueTextEquals("notes");
            bool isLogin = reader.ValueTextEquals("login");
            bool isFields = reader.ValueTextEquals("fields");

            reader.ReadNextTokenOrThrow("Encrypted cipher payload ended unexpectedly.");

            if (isKey)
            {
                reader.SkipValue();
            }
            else if (isName)
            {
                name = ReadOptionalDecryptedString(ref reader, key);
            }
            else if (isNotes)
            {
                notes = ReadOptionalDecryptedString(ref reader, key);
            }
            else if (isLogin)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    LoginContent login = ReadLoginObject(ref reader, key);
                    username = login.Username;
                    password = login.Password;
                    uris = login.Uris;
                }
                else
                {
                    username = null;
                    password = null;
                    uris = [];
                    reader.SkipValue();
                }
            }
            else if (isFields)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    fields = ReadFieldsArray(ref reader, key);
                }
                else
                {
                    fields = [];
                    reader.SkipValue();
                }
            }
            else
            {
                reader.SkipValue();
            }
        }

        throw new JsonException("Encrypted cipher payload ended unexpectedly.");
    }

    private LoginContent ReadLoginObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> key)
    {
        string? username = null;
        string? password = null;
        List<string> uris = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new LoginContent(username, password, uris);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Encrypted cipher login payload contained an unexpected token.");
            }

            bool isUsername = reader.ValueTextEquals("username");
            bool isPassword = reader.ValueTextEquals("password");
            bool isUris = reader.ValueTextEquals("uris");

            reader.ReadNextTokenOrThrow("Encrypted cipher login payload ended unexpectedly.");

            if (isUsername)
            {
                username = ReadOptionalDecryptedString(ref reader, key);
            }
            else if (isPassword)
            {
                password = ReadOptionalDecryptedString(ref reader, key);
            }
            else if (isUris)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    uris = ReadUrisArray(ref reader, key);
                }
                else
                {
                    uris = [];
                    reader.SkipValue();
                }
            }
            else
            {
                reader.SkipValue();
            }
        }

        throw new JsonException("Encrypted cipher login payload ended unexpectedly.");
    }

    private List<string> ReadUrisArray(ref Utf8JsonReader reader, ReadOnlySpan<byte> key)
    {
        List<string> uris = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return uris;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string? uri = ReadUriObject(ref reader, key);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    uris.Add(uri);
                }
            }
            else
            {
                reader.SkipValue();
            }
        }

        throw new JsonException("Encrypted cipher URI array ended unexpectedly.");
    }

    private string? ReadUriObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> key)
    {
        string? uri = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return uri;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Encrypted cipher URI payload contained an unexpected token.");
            }

            bool isUri = reader.ValueTextEquals("uri");
            reader.ReadNextTokenOrThrow("Encrypted cipher URI payload ended unexpectedly.");

            if (isUri)
            {
                uri = ReadOptionalDecryptedString(ref reader, key);
            }
            else
            {
                reader.SkipValue();
            }
        }

        throw new JsonException("Encrypted cipher URI payload ended unexpectedly.");
    }

    private List<DecryptedCustomField> ReadFieldsArray(ref Utf8JsonReader reader, ReadOnlySpan<byte> key)
    {
        List<DecryptedCustomField> fields = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return fields;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                fields.Add(ReadFieldObject(ref reader, key));
            }
            else
            {
                reader.SkipValue();
                fields.Add(new DecryptedCustomField(null, null, null));
            }
        }

        throw new JsonException("Encrypted cipher fields array ended unexpectedly.");
    }

    private DecryptedCustomField ReadFieldObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> key)
    {
        string? name = null;
        string? value = null;
        int? type = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new DecryptedCustomField(name, value, type);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Encrypted cipher field payload contained an unexpected token.");
            }

            bool isName = reader.ValueTextEquals("name");
            bool isValue = reader.ValueTextEquals("value");
            bool isType = reader.ValueTextEquals("type");

            reader.ReadNextTokenOrThrow("Encrypted cipher field payload ended unexpectedly.");

            if (isName)
            {
                name = ReadOptionalDecryptedString(ref reader, key);
            }
            else if (isValue)
            {
                value = ReadOptionalDecryptedString(ref reader, key);
            }
            else if (isType)
            {
                type = reader.ReadOptionalInt32();
            }
            else
            {
                reader.SkipValue();
            }
        }

        throw new JsonException("Encrypted cipher field payload ended unexpectedly.");
    }

    private string? ReadOptionalDecryptedString(ref Utf8JsonReader reader, ReadOnlySpan<byte> key)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            reader.SkipValue();
            return null;
        }

        using var encrypted = EncString.FromJsonStringToken(ref reader);
        if (encrypted.AsSpan().IsEmpty)
        {
            return encrypted.ToString();
        }

        return cryptoService.DecryptString(encrypted, key);
    }

    private DecryptedCipher DecryptCipherCore(CipherSyncItem record, byte[] userKey)
    {
        byte[]? cipherKey = null;

        try
        {
            Utf8JsonReader reader = new(record.EncryptedPayload, isFinalBlock: true, state: default);
            reader.ReadRequiredStartObject("Encrypted cipher payload root was not an object.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    reader.EnsureNoTrailingData();

                    CipherContent content = ReadCipherContent(record.EncryptedPayload, cipherKey ?? userKey);
                    return new DecryptedCipher(
                        record.Id,
                        (CipherType)record.Type,
                        content.Name,
                        content.Username,
                        content.Password,
                        content.Notes,
                        content.Uris,
                        content.Fields,
                        record.FolderId,
                        record.OrganizationId,
                        record.RevisionDate);
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Encrypted cipher payload contained an unexpected token.");
                }

                bool isKey = reader.ValueTextEquals("key");
                reader.ReadNextTokenOrThrow("Encrypted cipher payload ended unexpectedly.");

                if (!isKey)
                {
                    reader.SkipValue();
                    continue;
                }

                byte[]? nextKey = null;
                if (reader.TokenType == JsonTokenType.String)
                {
                    using EncString encryptedKey = EncString.FromJsonStringToken(ref reader);
                    nextKey = cryptoService.UnwrapKey(encryptedKey, userKey);
                }
                else
                {
                    reader.SkipValue();
                }

                CryptographicOperations.ZeroMemory(cipherKey);
                cipherKey = nextKey;
            }

            throw new JsonException("Encrypted cipher payload ended unexpectedly.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    private readonly record struct CipherContent(
        string? Name,
        string? Username,
        string? Password,
        string? Notes,
        List<string> Uris,
        List<DecryptedCustomField> Fields);

    private readonly record struct LoginContent(
        string? Username,
        string? Password,
        List<string> Uris);
}
