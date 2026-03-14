using System.Security.Cryptography;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;
using Microsoft.Extensions.Logging;

namespace BitwaredApi.Services;

internal sealed class CipherPayloadDecryptor(
    ICryptoService cryptoService,
    ILogger<CipherPayloadDecryptor> logger)
    : ICipherPayloadDecryptor
{
    private const string CachedVaultDecryptionFailedMessage = "Cached vault data could not be decrypted.";

    public VaultDecryptionOutcome<DecryptedCipher> DecryptCipher(
        CipherSyncItem item,
        Stream payload,
        byte[] userKey)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(userKey);

        try
        {
            return new VaultDecryptionOutcome<DecryptedCipher>.Success(
                DecryptCipherCore(item, payload, userKey));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse cached cipher payload for cipher {CipherId} of type {CipherType}.",
                item.Id,
                item.Type);

            return new VaultDecryptionOutcome<DecryptedCipher>.Failed(CachedVaultDecryptionFailedMessage);
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to decrypt cached cipher payload for cipher {CipherId} of type {CipherType}.",
                item.Id,
                item.Type);

            return new VaultDecryptionOutcome<DecryptedCipher>.Failed(CachedVaultDecryptionFailedMessage);
        }
    }

    private DecryptedCipher DecryptCipherCore(
        CipherSyncItem item,
        Stream payload,
        byte[] userKey)
    {
        if (!payload.CanSeek)
        {
            throw new InvalidOperationException("Cipher payload stream must be seekable.");
        }

        byte[]? cipherKey = null;

        try
        {
            cipherKey = ReadWrappedCipherKey(payload, userKey);
            payload.Seek(0, SeekOrigin.Begin);

            CipherContent content = ReadCipherContent(payload, cipherKey ?? userKey);
            return new DecryptedCipher(
                item.Id,
                (CipherType)item.Type,
                content.Name,
                content.Username,
                content.Password,
                content.Notes,
                content.Uris,
                content.Fields,
                item.FolderId,
                item.OrganizationId,
                item.RevisionDate);
        }
        finally
        {
            if (cipherKey is not null)
            {
                CryptographicOperations.ZeroMemory(cipherKey);
            }
        }
    }

    private byte[]? ReadWrappedCipherKey(Stream payload, byte[] userKey)
    {
        WrappedCipherKeyParseState state = new(cryptoService, userKey);

        try
        {
            Utf8JsonStreamParser.Parse(
                payload,
                state,
                ProcessWrappedCipherKeyPass);

            state.ValidateCompleted();

            return state.TakeCipherKey();
        }
        finally
        {
            state.Dispose();
        }
    }

    private CipherContent ReadCipherContent(Stream payload, byte[] key)
    {
        CipherContentParseState state = new(cryptoService, key);

        Utf8JsonStreamParser.Parse(
            payload,
            state,
            ProcessCipherContentPass);

        state.ValidateCompleted();

        return state.BuildContent();
    }

    private static void ProcessWrappedCipherKeyPass(
        WrappedCipherKeyParseState state,
        ref Utf8JsonReader reader,
        ReadOnlySpan<byte> buffer)
        => state.ProcessPass(ref reader);

    private static void ProcessCipherContentPass(
        CipherContentParseState state,
        ref Utf8JsonReader reader,
        ReadOnlySpan<byte> buffer)
        => state.ProcessPass(ref reader);

    private readonly record struct CipherContent(
        string? Name,
        string? Username,
        string? Password,
        string? Notes,
        List<string> Uris,
        List<DecryptedCustomField> Fields);

    private enum RootProperty : byte
    {
        None,
        Key,
        Unknown,
    }

    private enum CipherContainer : byte
    {
        None,
        RootObject,
        LoginObject,
        UrisArray,
        UriObject,
        FieldsArray,
        FieldObject,
    }

    private enum ContentProperty : byte
    {
        None,
        Unknown,
        Key,
        Name,
        Notes,
        Login,
        Fields,
        Username,
        Password,
        Uris,
        Uri,
        FieldName,
        FieldValue,
        FieldType,
    }

    private sealed class WrappedCipherKeyParseState(
        ICryptoService cryptoService,
        byte[] userKey) : IDisposable
    {
        private byte[]? _cipherKey;

        public bool RootStarted { get; private set; }
        public bool RootCompleted { get; private set; }
        public RootProperty PendingProperty { get; private set; }
        public int SkipDepth { get; private set; }

        public void ProcessPass(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                ProcessToken(ref reader);
            }
        }

        public void ValidateCompleted()
        {
            if (!RootStarted || !RootCompleted || PendingProperty != RootProperty.None || SkipDepth != 0)
            {
                throw new JsonException("Encrypted cipher payload ended unexpectedly.");
            }
        }

        public byte[]? TakeCipherKey()
        {
            byte[]? cipherKey = _cipherKey;
            _cipherKey = null;
            return cipherKey;
        }

        public void Dispose()
        {
            if (_cipherKey is not null)
            {
                CryptographicOperations.ZeroMemory(_cipherKey);
                _cipherKey = null;
            }
        }

        private void ProcessToken(ref Utf8JsonReader reader)
        {
            if (!RootStarted)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Encrypted cipher payload root was not an object.");
                }

                RootStarted = true;
                return;
            }

            if (RootCompleted)
            {
                throw new JsonException("Encrypted cipher payload contained trailing data.");
            }

            if (SkipDepth > 0)
            {
                int skipDepth = SkipDepth;
                Utf8JsonStreamParser.UpdateDepth(ref skipDepth, reader.TokenType);
                SkipDepth = skipDepth;
                return;
            }

            if (PendingProperty != RootProperty.None)
            {
                HandlePendingPropertyValue(ref reader);
                PendingProperty = RootProperty.None;
                return;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                PendingProperty = reader.ValueTextEquals("key")
                    ? RootProperty.Key
                    : RootProperty.Unknown;
                return;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                RootCompleted = true;
                return;
            }

            throw new JsonException("Encrypted cipher payload contained an unexpected token.");
        }

        private void HandlePendingPropertyValue(ref Utf8JsonReader reader)
        {
            if (PendingProperty == RootProperty.Key && reader.TokenType == JsonTokenType.String)
            {
                using EncString encryptedKey = EncString.FromJsonStringToken(ref reader);
                byte[] nextKey = cryptoService.UnwrapKey(encryptedKey, userKey);

                if (_cipherKey is not null)
                {
                    CryptographicOperations.ZeroMemory(_cipherKey);
                }

                _cipherKey = nextKey;
                return;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                SkipDepth = 1;
            }
        }
    }

    private sealed class CipherContentParseState(
        ICryptoService cryptoService,
        byte[] key)
    {
        private readonly List<CipherContainer> _containers = [];

        public bool RootStarted { get; private set; }
        public bool RootCompleted { get; private set; }
        public int SkipDepth { get; private set; }
        public ContentProperty PendingProperty { get; private set; }
        public string? Name { get; private set; }
        public string? Notes { get; private set; }
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public List<string> Uris { get; } = [];
        public List<DecryptedCustomField> Fields { get; } = [];
        public string? CurrentUri { get; private set; }
        public string? CurrentFieldName { get; private set; }
        public string? CurrentFieldValue { get; private set; }
        public int? CurrentFieldType { get; private set; }

        private CipherContainer CurrentContainer => _containers.Count == 0 ? CipherContainer.None : _containers[^1];
        private bool HasOpenContainers => _containers.Count > 0;

        public void ProcessPass(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                ProcessToken(ref reader);
            }
        }

        public void ValidateCompleted()
        {
            if (!RootStarted
                || !RootCompleted
                || SkipDepth != 0
                || PendingProperty != ContentProperty.None
                || HasOpenContainers)
            {
                throw new JsonException("Encrypted cipher payload ended unexpectedly.");
            }
        }

        public CipherContent BuildContent()
            => new(Name, Username, Password, Notes, Uris, Fields);

        private void ProcessToken(ref Utf8JsonReader reader)
        {
            if (!RootStarted)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Encrypted cipher payload root was not an object.");
                }

                RootStarted = true;
                Enter(CipherContainer.RootObject);
                return;
            }

            if (RootCompleted)
            {
                throw new JsonException("Encrypted cipher payload contained trailing data.");
            }

            if (SkipDepth > 0)
            {
                int skipDepth = SkipDepth;
                Utf8JsonStreamParser.UpdateDepth(ref skipDepth, reader.TokenType);
                SkipDepth = skipDepth;
                return;
            }

            if (PendingProperty != ContentProperty.None)
            {
                HandlePendingPropertyValue(ref reader);
                PendingProperty = ContentProperty.None;
                return;
            }

            switch (CurrentContainer)
            {
                case CipherContainer.RootObject:
                case CipherContainer.LoginObject:
                case CipherContainer.UriObject:
                case CipherContainer.FieldObject:
                    HandleObjectToken(ref reader);
                    return;

                case CipherContainer.UrisArray:
                    HandleUrisArrayToken(ref reader);
                    return;

                case CipherContainer.FieldsArray:
                    HandleFieldsArrayToken(ref reader);
                    return;

                default:
                    throw new JsonException("Encrypted cipher payload contained an unexpected token.");
            }
        }

        private void HandleObjectToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                PendingProperty = MapProperty(CurrentContainer, ref reader);
                return;
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("Encrypted cipher payload contained an unexpected token.");
            }

            switch (CurrentContainer)
            {
                case CipherContainer.RootObject:
                    Exit(CipherContainer.RootObject);
                    RootCompleted = true;
                    break;

                case CipherContainer.LoginObject:
                    Exit(CipherContainer.LoginObject);
                    break;

                case CipherContainer.UriObject:
                    if (!string.IsNullOrWhiteSpace(CurrentUri))
                    {
                        Uris.Add(CurrentUri);
                    }

                    CurrentUri = null;
                    Exit(CipherContainer.UriObject);
                    break;

                case CipherContainer.FieldObject:
                    Fields.Add(new DecryptedCustomField(CurrentFieldName, CurrentFieldValue, CurrentFieldType));
                    CurrentFieldName = null;
                    CurrentFieldValue = null;
                    CurrentFieldType = null;
                    Exit(CipherContainer.FieldObject);
                    break;
            }
        }

        private void HandleUrisArrayToken(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    CurrentUri = null;
                    Enter(CipherContainer.UriObject);
                    break;

                case JsonTokenType.EndArray:
                    Exit(CipherContainer.UrisArray);
                    break;

                case JsonTokenType.StartArray:
                    SkipDepth = 1;
                    break;
            }
        }

        private void HandleFieldsArrayToken(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    CurrentFieldName = null;
                    CurrentFieldValue = null;
                    CurrentFieldType = null;
                    Enter(CipherContainer.FieldObject);
                    break;

                case JsonTokenType.EndArray:
                    Exit(CipherContainer.FieldsArray);
                    break;

                case JsonTokenType.StartArray:
                    SkipDepth = 1;
                    Fields.Add(new DecryptedCustomField(null, null, null));
                    break;

                default:
                    Fields.Add(new DecryptedCustomField(null, null, null));
                    break;
            }
        }

        private void HandlePendingPropertyValue(ref Utf8JsonReader reader)
        {
            switch (PendingProperty)
            {
                case ContentProperty.Name:
                    Name = ReadOptionalEncryptedString(ref reader, "name");
                    break;

                case ContentProperty.Notes:
                    Notes = ReadOptionalEncryptedString(ref reader, "notes");
                    break;

                case ContentProperty.Login:
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        Enter(CipherContainer.LoginObject);
                    }
                    else if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        SkipDepth = 1;
                    }
                    break;

                case ContentProperty.Fields:
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        Enter(CipherContainer.FieldsArray);
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        SkipDepth = 1;
                    }
                    break;

                case ContentProperty.Username:
                    Username = ReadOptionalEncryptedString(ref reader, "username");
                    break;

                case ContentProperty.Password:
                    Password = ReadOptionalEncryptedString(ref reader, "password");
                    break;

                case ContentProperty.Uris:
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        Enter(CipherContainer.UrisArray);
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        SkipDepth = 1;
                    }
                    break;

                case ContentProperty.Uri:
                    CurrentUri = ReadOptionalEncryptedString(ref reader, "uri");
                    break;

                case ContentProperty.FieldName:
                    CurrentFieldName = ReadOptionalEncryptedString(ref reader, "field.name");
                    break;

                case ContentProperty.FieldValue:
                    CurrentFieldValue = ReadOptionalEncryptedString(ref reader, "field.value");
                    break;

                case ContentProperty.FieldType:
                    CurrentFieldType = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int type)
                        ? type
                        : null;

                    if (CurrentFieldType is null && reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    {
                        SkipDepth = 1;
                    }

                    break;

                case ContentProperty.Key:
                case ContentProperty.Unknown:
                    if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    {
                        SkipDepth = 1;
                    }

                    break;
            }
        }

        private string? ReadOptionalEncryptedString(ref Utf8JsonReader reader, string propertyName)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Encrypted cipher payload property '{propertyName}' was not a string or null.");
            }

            using EncString encrypted = EncString.FromJsonStringToken(ref reader);
            if (encrypted.AsSpan().IsEmpty)
            {
                return encrypted.ToString();
            }

            return cryptoService.DecryptString(encrypted, key);
        }

        private void Enter(CipherContainer container)
            => _containers.Add(container);

        private void Exit(CipherContainer expected)
        {
            if (CurrentContainer != expected)
            {
                throw new JsonException("Encrypted cipher payload contained an unexpected token.");
            }

            _containers.RemoveAt(_containers.Count - 1);
        }
    }

    private static ContentProperty MapProperty(CipherContainer container, ref Utf8JsonReader reader)
        => container switch
        {
            CipherContainer.RootObject when reader.ValueTextEquals("key") => ContentProperty.Key,
            CipherContainer.RootObject when reader.ValueTextEquals("name") => ContentProperty.Name,
            CipherContainer.RootObject when reader.ValueTextEquals("notes") => ContentProperty.Notes,
            CipherContainer.RootObject when reader.ValueTextEquals("login") => ContentProperty.Login,
            CipherContainer.RootObject when reader.ValueTextEquals("fields") => ContentProperty.Fields,
            CipherContainer.LoginObject when reader.ValueTextEquals("username") => ContentProperty.Username,
            CipherContainer.LoginObject when reader.ValueTextEquals("password") => ContentProperty.Password,
            CipherContainer.LoginObject when reader.ValueTextEquals("uris") => ContentProperty.Uris,
            CipherContainer.UriObject when reader.ValueTextEquals("uri") => ContentProperty.Uri,
            CipherContainer.FieldObject when reader.ValueTextEquals("name") => ContentProperty.FieldName,
            CipherContainer.FieldObject when reader.ValueTextEquals("value") => ContentProperty.FieldValue,
            CipherContainer.FieldObject when reader.ValueTextEquals("type") => ContentProperty.FieldType,
            _ => ContentProperty.Unknown,
        };
}
