using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Attachments.Contracts;
using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

/// <summary>
/// Parses vault cipher payloads using parser-local state. Create one instance for each
/// sequential vault open operation; instances are not reentrant or thread-safe.
/// </summary>
internal sealed partial class VaultDataParser
{
    private readonly DecryptedValueJsonConverter<string> _stringConverter;
    private readonly DecryptedValueJsonConverter<string?> _nullableStringConverter;
    private readonly DecryptedValueJsonConverter<byte[]> _guidConverter;
    private readonly DecryptedValueJsonConverter<byte[]> _base64UrlConverter;
    private readonly DecryptedValueJsonConverter<uint> _numberConverter;
    private readonly DecryptedValueJsonConverter<bool> _booleanConverter;
    private readonly DecryptedValueJsonConverter<TotpValue?> _totpConverter;
    private readonly DecryptedValueJsonConverter<OpenSshPublicKey> _sshPublicKeyConverter;
    private readonly DecryptedValueJsonConverter<Fido2CredentialKeyType> _keyTypeConverter;
    private readonly DecryptedValueJsonConverter<Fido2CredentialKeyAlgorithm> _keyAlgorithmConverter;
    private readonly DecryptedValueJsonConverter<Fido2CredentialKeyCurve> _keyCurveConverter;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly LoginUrisJsonConverter _loginUrisConverter;
    private readonly FirstFido2CredentialJsonConverter _firstFido2CredentialConverter;

    private VaultCipher? _activeCipher;

    public VaultDataParser()
    {
        _stringConverter = new DecryptedValueJsonConverter<string>(default, VaultPayloadValueParsers.ParseString);
        _nullableStringConverter = new DecryptedValueJsonConverter<string?>(default, VaultPayloadValueParsers.ParseString, allowNull: true);
        _guidConverter = new DecryptedValueJsonConverter<byte[]>(default, VaultPayloadValueParsers.ParseGuid);
        _base64UrlConverter = new(default, VaultPayloadValueParsers.ParseBase64Url);
        _numberConverter = new(default, VaultPayloadValueParsers.ParseNumber<uint>);
        _booleanConverter = new(default, VaultPayloadValueParsers.ParseBoolean);
        _totpConverter = new(default, VaultPayloadValueParsers.ParseTotp, allowNull: true);
        _sshPublicKeyConverter = new(default, VaultPayloadValueParsers.ParseSshPublicKey);
        _keyTypeConverter = new(default, static value => Fido2CredentialEnumExtensions.ParseKeyType(value));
        _keyAlgorithmConverter = new(default, static value => Fido2CredentialEnumExtensions.ParseKeyAlgorithm(value));
        _keyCurveConverter = new(default, static value => Fido2CredentialEnumExtensions.ParseKeyCurve(value));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            RespectNullableAnnotations = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            TypeInfoResolver = VaultPayloadJsonContext.Default.WithAddedModifier(ConfigureContract)
        };

        _jsonOptions.Converters.Add(_stringConverter);
        _jsonOptions.Converters.Add(_base64UrlConverter);
        _jsonOptions.Converters.Add(_numberConverter);
        _jsonOptions.Converters.Add(_booleanConverter);
        _jsonOptions.Converters.Add(_totpConverter);
        _jsonOptions.Converters.Add(_sshPublicKeyConverter);
        _jsonOptions.Converters.Add(_keyTypeConverter);
        _jsonOptions.Converters.Add(_keyAlgorithmConverter);
        _jsonOptions.Converters.Add(_keyCurveConverter);
        _jsonOptions.Converters.Add(new UriMatchTypeJsonConverter());

        var loginUriTypeInfo = (JsonTypeInfo<LoginUri>)_jsonOptions.GetTypeInfo(typeof(LoginUri));
        var fido2CredentialTypeInfo = (JsonTypeInfo<Fido2Credential>)_jsonOptions.GetTypeInfo(typeof(Fido2Credential));
        _firstFido2CredentialConverter = new FirstFido2CredentialJsonConverter(fido2CredentialTypeInfo);
        _loginUrisConverter = new LoginUrisJsonConverter(GetActiveLoginUris, loginUriTypeInfo);
    }

    public VaultCipher ParseAndDecryptCipher(
        ref readonly VaultCipherResponse dto,
        ReadOnlySpan<byte> payload,
        SymmetricCryptoKey baseKey)
    {
        if (_activeCipher is not null)
            throw new InvalidOperationException("Vault payload parsing is sequential and non-reentrant.");

        VaultCipher cipher = VaultCipher.CreateBlankCipher(dto.VaultCipherType);
        cipher.Id = dto.Id;
        cipher.FolderId = dto.FolderId;
        cipher.Favorite = dto.Favorite;
        cipher.Reprompt = dto.Reprompt;
        cipher.RevisionDate = dto.RevisionDate;
        cipher.CreationDate = dto.CreationDate;
        cipher.DeletedDate = dto.DeletedDate;

        var reader = new Utf8JsonReader(payload, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object payload.");

        using var key = CipherKey.Create(dto.ProtectedCipherKey, baseKey);
        SetActiveKey(key.BorrowKeyMemory());
        _activeCipher = cipher;
        try
        {
            VaultCipher parsedCipher = (VaultCipher)(JsonSerializer.Deserialize(
                ref reader, _jsonOptions.GetTypeInfo(cipher.GetType()))
                ?? throw new JsonException("Vault payload cannot be null."));

            parsedCipher.Attachments = ParseAttachments(dto.Id, dto.Attachments, key);
            return parsedCipher;
        }
        finally
        {
            _activeCipher = null;
            SetActiveKey(default);
        }
    }

    private void ConfigureContract(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(LoginUri))
        {
            JsonPropertyInfo uri = GetProperty(typeInfo, nameof(LoginUri.Value));
            uri.Name = "uri";
            uri.IsRequired = true;
            uri.CustomConverter = _stringConverter;
            return;
        }

        if (typeInfo.Type == typeof(Fido2Credential))
        {
            GetProperty(typeInfo, nameof(Fido2Credential.CredentialId)).CustomConverter = _guidConverter;
            return;
        }

        if (typeof(VaultCipher).IsAssignableFrom(typeInfo.Type) && !typeInfo.Type.IsAbstract)
            ConfigureCipherContract(typeInfo);
    }

    private void ConfigureCipherContract(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(LoginVaultCipher))
        {
            GetProperty(typeInfo, nameof(LoginVaultCipher.Username)).CustomConverter = _stringConverter;
            GetProperty(typeInfo, nameof(LoginVaultCipher.Password)).CustomConverter = _stringConverter;
            GetProperty(typeInfo, nameof(LoginVaultCipher.Uris)).CustomConverter = _loginUrisConverter;
            JsonPropertyInfo fido2 = GetProperty(typeInfo, nameof(LoginVaultCipher.Fido2Credential));
            fido2.Name = "fido2Credentials";
            fido2.CustomConverter = _firstFido2CredentialConverter;
        }

        for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            JsonPropertyInfo property = typeInfo.Properties[i];
            bool commonProperty = property.DeclaringType == typeof(VaultCipher);
            bool isCommonPayloadField = IsProperty(property, nameof(VaultCipher.Name)) || IsProperty(property, nameof(VaultCipher.Notes));
            if ((commonProperty && !isCommonPayloadField) || (!commonProperty && property.Set is null))
            {
                typeInfo.Properties.RemoveAt(i);
                continue;
            }

            property.IsRequired = false;
            if (property.CustomConverter is null && property.PropertyType == typeof(string) && property.IsSetNullable)
                property.CustomConverter = _nullableStringConverter;
        }

        typeInfo.CreateObject = () =>
        {
            VaultCipher active = _activeCipher
                ?? throw new InvalidOperationException("No active cipher is being parsed.");
            if (active.GetType() != typeInfo.Type)
                throw new InvalidOperationException("The active cipher type does not match the JSON contract.");
            return active;
        };
    }

    private static JsonPropertyInfo GetProperty(JsonTypeInfo typeInfo, string propertyName)
        => typeInfo.Properties.FirstOrDefault(property =>
               string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"JSON property '{propertyName}' was not generated for {typeInfo.Type.Name}.");

    private static bool IsProperty(JsonPropertyInfo property, string name)
        => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);

    private void SetActiveKey(ReadOnlyMemory<byte> key)
    {
        _stringConverter.Key = key;
        _nullableStringConverter.Key = key;
        _guidConverter.Key = key;
        _base64UrlConverter.Key = key;
        _numberConverter.Key = key;
        _booleanConverter.Key = key;
        _totpConverter.Key = key;
        _sshPublicKeyConverter.Key = key;
        _keyTypeConverter.Key = key;
        _keyAlgorithmConverter.Key = key;
        _keyCurveConverter.Key = key;
    }

    private List<LoginUri> GetActiveLoginUris()
        => _activeCipher is LoginVaultCipher login
            ? login.Uris
            : throw new InvalidOperationException("No active login cipher is available.");

    private static VaultCipherAttachment[] ParseAttachments(
        CipherId cipherId,
        ReadOnlySpan<VaultCipherAttachmentDownloadResponse> attachmentDtos,
        scoped in CipherKey decryptionKey)
    {
        if (attachmentDtos is not { Length: > 0 })
            return [];

        var attachments = new VaultCipherAttachment[attachmentDtos.Length];
        for (int i = 0; i < attachmentDtos.Length; i++)
        {
            ref readonly var dto = ref attachmentDtos[i];
            attachments[i] = new VaultCipherAttachment
            {
                Id = dto.Id,
                CipherId = cipherId,
                FileName = dto.EncryptedFileName.Decode(decryptionKey),
                Size = dto.Size
            };
        }

        return attachments;
    }
}
