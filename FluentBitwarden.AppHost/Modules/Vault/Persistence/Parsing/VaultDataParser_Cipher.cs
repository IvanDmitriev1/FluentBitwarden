using System.Security.Cryptography;
using System.Text.Json;
using BitwardenApi.Vault.Attachments.Contracts;
using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

public static partial class VaultDataParser
{
    public static VaultCipher ParseAndDecryptCipher(
        ref readonly VaultCipherDto dto,
        ReadOnlySpan<byte> payload,
        scoped ReadOnlySpan<byte> baseKey)
    {
        var cipher = CreateCipher(in dto);
        var reader = CreateObjectReader(payload);

        if (dto.EncryptedKey.IsEmpty)
        {
            return ParseWithKey(in dto, cipher, ref reader, baseKey);
        }

        var encryptedKey = dto.EncryptedKey;
        bool useStackBuffer = encryptedKey.MaxPlaintextByteCount <= 256;

        using var keyBufferOwner = useStackBuffer
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(encryptedKey.MaxPlaintextByteCount);

        Span<byte> keyBuffer = useStackBuffer
            ? stackalloc byte[encryptedKey.MaxPlaintextByteCount]
            : keyBufferOwner.Span;

        try
        {
            int bytesWritten = encryptedKey.DecodeTo(baseKey, keyBuffer);
            return ParseWithKey(in dto, cipher, ref reader, keyBuffer[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBuffer);
        }
    }

    private static VaultCipher ParseWithKey(
        ref readonly VaultCipherDto dto,
        VaultCipher cipher,
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> decryptionKey)
    {
        VaultCipher parsedCipher = dto.VaultCipherType switch
        {
            VaultCipherType.Login => ParseLoginCipher((LoginVaultCipher)cipher, ref reader, decryptionKey),
            VaultCipherType.SecureNote => ParseSecureNoteCipher((SecureNoteVaultCipher)cipher, ref reader, decryptionKey),
            VaultCipherType.Card => ParseCardCipher((CardVaultCipher)cipher, ref reader, decryptionKey),
            VaultCipherType.Identity => ParseIdentityCipher((IdentityVaultCipher)cipher, ref reader, decryptionKey),
            VaultCipherType.SshKey => ParseSshKeyCipher((SshKeyVaultCipher)cipher, ref reader, decryptionKey),
            _ => throw new NotSupportedException($"Unsupported vaultCipher type: {dto.VaultCipherType}")
        };

        parsedCipher.Attachments = ParseAttachments(dto.Id, dto.Attachments, decryptionKey);
        return parsedCipher;
    }

    private static VaultCipherAttachment[] ParseAttachments(
        CipherId cipherId,
        ReadOnlySpan<VaultCipherAttachmentDownloadResponse> attachmentDtos,
        scoped ReadOnlySpan<byte> decryptionKey)
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

    private static LoginVaultCipher ParseLoginCipher(LoginVaultCipher vaultCipher, ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> decryptKey)
        => ParseCipherObject(vaultCipher, ref reader, decryptKey,
            static (ref r, c, scoped k) =>
            {
                if (r.ValueTextEquals("username"u8) || r.ValueTextEquals("Username"u8))
                    c.Username = ReadRequiredDecryptField(ref r, k, "Username");
                else if (r.ValueTextEquals("password"u8) || r.ValueTextEquals("Password"u8))
                    c.Password = ReadRequiredDecryptField(ref r, k, "Password");
                else if (r.ValueTextEquals("totp"u8) || r.ValueTextEquals("Totp"u8))
                    c.Totp = ReadTotpCredential(ref r, k);
                else if (r.ValueTextEquals("uris"u8) || r.ValueTextEquals("Uris"u8))
                    c.Uris = ReadJsonArray(ref r, k, ReadUri);
                else if (r.ValueTextEquals("fido2Credentials"u8) || r.ValueTextEquals("Fido2Credentials"u8))
                    c.Fido2Credential = ReadFirstFido2Credential(ref r, k);
                else
                    return false;

                return true;
            });


    private static SecureNoteVaultCipher ParseSecureNoteCipher(SecureNoteVaultCipher vaultCipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
        => ParseCipherObject(vaultCipher, ref reader, decryptKey, static (ref _, _, scoped _) => false);

    private static CardVaultCipher ParseCardCipher(CardVaultCipher vaultCipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
        => ParseCipherObject(vaultCipher, ref reader, decryptKey, static (ref r, c, scoped k) =>
        {
            if (r.ValueTextEquals("cardholderName"u8) || r.ValueTextEquals("CardholderName"u8))
                c.CardholderName = ReadDecryptField(ref r, k);
            else if (r.ValueTextEquals("brand"u8) || r.ValueTextEquals("Brand"u8))
                c.Brand = ReadDecryptField(ref r, k);
            else if (r.ValueTextEquals("number"u8) || r.ValueTextEquals("Number"u8))
                c.Number = ReadDecryptField(ref r, k);
            else if (r.ValueTextEquals("expMonth"u8) || r.ValueTextEquals("ExpMonth"u8))
                c.ExpMonth = ReadDecryptField(ref r, k);
            else if (r.ValueTextEquals("expYear"u8) || r.ValueTextEquals("ExpYear"u8))
                c.ExpYear = ReadDecryptField(ref r, k);
            else if (r.ValueTextEquals("code"u8) || r.ValueTextEquals("Code"u8))
                c.Code = ReadDecryptField(ref r, k);
            else
                return false;

            return true;
        });

    private static IdentityVaultCipher ParseIdentityCipher(IdentityVaultCipher vaultCipher, ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> decryptKey)
        => ParseCipherObject(vaultCipher, ref reader, decryptKey,
            static (ref r, c, scoped k) =>
            {
                if (r.ValueTextEquals("title"u8) || r.ValueTextEquals("Title"u8))
                    c.Title = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("firstName"u8) || r.ValueTextEquals("FirstName"u8))
                    c.FirstName = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("middleName"u8) || r.ValueTextEquals("MiddleName"u8))
                    c.MiddleName = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("lastName"u8) || r.ValueTextEquals("LastName"u8))
                    c.LastName = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("address1"u8) || r.ValueTextEquals("Address1"u8))
                    c.Address1 = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("address2"u8) || r.ValueTextEquals("Address2"u8))
                    c.Address2 = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("address3"u8) || r.ValueTextEquals("Address3"u8))
                    c.Address3 = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("city"u8) || r.ValueTextEquals("City"u8))
                    c.City = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("state"u8) || r.ValueTextEquals("State"u8))
                    c.State = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("postalCode"u8) || r.ValueTextEquals("PostalCode"u8))
                    c.PostalCode = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("country"u8) || r.ValueTextEquals("Country"u8))
                    c.Country = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("company"u8) || r.ValueTextEquals("Company"u8))
                    c.Company = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("email"u8) || r.ValueTextEquals("Email"u8))
                    c.Email = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("phone"u8) || r.ValueTextEquals("Phone"u8))
                    c.Phone = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("ssn"u8) || r.ValueTextEquals("Ssn"u8))
                    c.Ssn = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("username"u8) || r.ValueTextEquals("Username"u8))
                    c.Username = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("passportNumber"u8) || r.ValueTextEquals("PassportNumber"u8))
                    c.PassportNumber = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("licenseNumber"u8) || r.ValueTextEquals("LicenseNumber"u8))
                    c.LicenseNumber = ReadDecryptField(ref r, k);
                else
                    return false;
                return true;
            });

    private static SshKeyVaultCipher ParseSshKeyCipher(SshKeyVaultCipher vaultCipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        return ParseCipherObject(vaultCipher, ref reader, decryptKey,
            static (ref r, c, scoped k) =>
            {
                if (r.ValueTextEquals("privateKey"u8) || r.ValueTextEquals("PrivateKey"u8))
                {
                    c.PrivateKey = ReadRequiredDecryptField(ref r, k, "PrivateKey");
                }
                else if (r.ValueTextEquals("publicKey"u8) || r.ValueTextEquals("PublicKey"u8))
                {
                    var rawKey = ReadRequiredDecryptField(ref r, k, "publicKey");
                    if (!OpenSshPublicKey.TryParse(rawKey, out var key))
                    {
                        //TODO Remove later
                        c.PublicKey = OpenSshPublicKey.CreateUnparsed(rawKey);
                    }

                    c.PublicKey = key;
                }
                else if (r.ValueTextEquals("keyFingerprint"u8) || r.ValueTextEquals("KeyFingerprint"u8))
                {
                    c.KeyFingerprint = ReadRequiredDecryptField(ref r, k, "KeyFingerprint");
                }
                else
                    return false;

                return true;
            });
    }

    private static bool TryReadCommonCipherProperty(ref Utf8JsonReader reader, VaultCipher vaultCipher, scoped ReadOnlySpan<byte> decryptKey)
    {
        if (reader.ValueTextEquals("name"u8) || reader.ValueTextEquals("Name"u8))
        {
            vaultCipher.Name = ReadRequiredDecryptField(ref reader, decryptKey, "name");
            return true;
        }

        if (reader.ValueTextEquals("notes"u8) || reader.ValueTextEquals("Notes"u8))
        {
            vaultCipher.Notes = ReadDecryptField(ref reader, decryptKey);
            return true;
        }

        return false;
    }

    private static VaultCipher CreateCipher(ref readonly VaultCipherDto dto) => dto.VaultCipherType switch
    {
        VaultCipherType.Login => new LoginVaultCipher
        {
            Id = dto.Id,
            FolderId = dto.FolderId,
            Name = string.Empty,
            Favorite = dto.Favorite,
            Reprompt = dto.Reprompt,
            RevisionDate = dto.RevisionDate,
            CreationDate = dto.CreationDate,
            DeletedDate = dto.DeletedDate,
        },
        VaultCipherType.SecureNote => new SecureNoteVaultCipher()
        {
            Id = dto.Id,
            FolderId = dto.FolderId,
            Name = string.Empty,
            Favorite = dto.Favorite,
            Reprompt = dto.Reprompt,
            RevisionDate = dto.RevisionDate,
            CreationDate = dto.CreationDate,
            DeletedDate = dto.DeletedDate
        },
        VaultCipherType.Card => new CardVaultCipher()
        {
            Id = dto.Id,
            FolderId = dto.FolderId,
            Name = string.Empty,
            Favorite = dto.Favorite,
            Reprompt = dto.Reprompt,
            RevisionDate = dto.RevisionDate,
            CreationDate = dto.CreationDate,
            DeletedDate = dto.DeletedDate
        },
        VaultCipherType.Identity => new IdentityVaultCipher()
        {
            Id = dto.Id,
            FolderId = dto.FolderId,
            Name = string.Empty,
            Favorite = dto.Favorite,
            Reprompt = dto.Reprompt,
            RevisionDate = dto.RevisionDate,
            CreationDate = dto.CreationDate,
            DeletedDate = dto.DeletedDate
        },
        VaultCipherType.SshKey => new SshKeyVaultCipher()
        {
            Id = dto.Id,
            FolderId = dto.FolderId,
            Name = string.Empty,
            Favorite = dto.Favorite,
            Reprompt = dto.Reprompt,
            RevisionDate = dto.RevisionDate,
            CreationDate = dto.CreationDate,
            DeletedDate = dto.DeletedDate
        },
        _ => throw new ArgumentOutOfRangeException()
    };

    private static LoginUri ReadUri(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Each URI item must be a JSON object.");

        string? uri = null;
        LoginUri.MatchType matchType = LoginUri.MatchType.Domain;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("uri"u8) || reader.ValueTextEquals("Uri"u8))
                uri = ReadDecryptField(ref reader, decryptKey);
            else if (reader.ValueTextEquals("match"u8) || reader.ValueTextEquals("Match"u8))
                matchType = ReadUriMatchType(ref reader);
            else
                SkipValue(ref reader);
        }

        return new LoginUri
        {
            Value = uri ?? throw new JsonException("URI item is missing Uri."),
            Match = matchType
        };
    }

    private static LoginUri.MatchType ReadUriMatchType(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("URI match cannot be NULL");

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
            throw new JsonException("URI match must be a valid Int32 value.");

        if (!Enum.IsDefined(typeof(LoginUri.MatchType), value))
            throw new JsonException($"Unsupported URI match type: {value}.");

        return (LoginUri.MatchType)value;
    }

    private static Fido2Credential? ReadFirstFido2Credential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a JSON array.");

        Fido2Credential? credential = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (credential is null)
            {
                credential = ReadFido2Credential(ref reader, decryptKey);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                reader.Skip();
        }

        return credential;
    }

    private static Fido2Credential ReadFido2Credential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Each fido2Credentials item must be a JSON object.");

        byte[]? credentialId = null;
        Fido2CredentialKeyType? keyType = null;
        Fido2CredentialKeyAlgorithm? keyAlgorithm = null;
        Fido2CredentialKeyCurve? keyCurve = null;
        byte[]? keyValue = null;
        string? rpId = null;
        string? rpName = null;
        byte[]? userHandle = null;
        string? userName = null;
        string? userDisplayName = null;
        int? counter = null;
        bool? discoverable = null;
        DateTimeOffset? creationDate = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("credentialId"u8) || reader.ValueTextEquals("CredentialId"u8))
                credentialId = Fido2CredentialJsonMapper.ReadCredentialId(ref reader, decryptKey, "CredentialId");
            else if (reader.ValueTextEquals("keyType"u8) || reader.ValueTextEquals("KeyType"u8))
                keyType = Fido2CredentialJsonMapper.ReadKeyType(ref reader, decryptKey, "KeyType");
            else if (reader.ValueTextEquals("keyAlgorithm"u8) || reader.ValueTextEquals("KeyAlgorithm"u8))
                keyAlgorithm = Fido2CredentialJsonMapper.ReadKeyAlgorithm(ref reader, decryptKey, "KeyAlgorithm");
            else if (reader.ValueTextEquals("keyCurve"u8) || reader.ValueTextEquals("KeyCurve"u8))
                keyCurve = Fido2CredentialJsonMapper.ReadKeyCurve(ref reader, decryptKey, "KeyCurve");
            else if (reader.ValueTextEquals("keyValue"u8) || reader.ValueTextEquals("KeyValue"u8))
                keyValue = ReadBase64UrlBytes(ref reader, decryptKey);
            else if (reader.ValueTextEquals("rpId"u8) || reader.ValueTextEquals("RpId"u8))
                rpId = ReadRequiredDecryptField(ref reader, decryptKey, "RpId");
            else if (reader.ValueTextEquals("rpName"u8) || reader.ValueTextEquals("RpName"u8))
                rpName = ReadRequiredDecryptField(ref reader, decryptKey, "RpName");
            else if (reader.ValueTextEquals("userHandle"u8) || reader.ValueTextEquals("UserHandle"u8))
                userHandle = ReadBase64UrlBytes(ref reader, decryptKey);
            else if (reader.ValueTextEquals("userName"u8) || reader.ValueTextEquals("UserName"u8))
                userName = ReadRequiredDecryptField(ref reader, decryptKey, "UserName");
            else if (reader.ValueTextEquals("userDisplayName"u8) || reader.ValueTextEquals("UserDisplayName"u8))
                userDisplayName = ReadRequiredDecryptField(ref reader, decryptKey, "UserDisplayName");
            else if (reader.ValueTextEquals("counter"u8) || reader.ValueTextEquals("Counter"u8))
                counter = ReadRequiredEncryptedInt32(ref reader, decryptKey);
            else if (reader.ValueTextEquals("discoverable"u8) || reader.ValueTextEquals("Discoverable"u8))
                discoverable = ReadRequiredEncryptedBoolean(ref reader, decryptKey);
            else if (reader.ValueTextEquals("creationDate"u8) || reader.ValueTextEquals("CreationDate"u8))
                creationDate = ReadRequiredDateTimeOffset(ref reader);
            else
                SkipValue(ref reader);
        }

        return new Fido2Credential
        {
            CredentialId = credentialId ?? throw new JsonException("FIDO2 credential is missing CredentialId."),
            KeyType = keyType ?? throw new JsonException("FIDO2 credential is missing KeyType."),
            KeyAlgorithm = keyAlgorithm ?? throw new JsonException("FIDO2 credential is missing KeyAlgorithm."),
            KeyCurve = keyCurve ?? throw new JsonException("FIDO2 credential is missing KeyCurve."),
            KeyValue = keyValue ?? throw new JsonException("FIDO2 credential is missing KeyValue."),
            RpId = rpId ?? throw new JsonException("FIDO2 credential is missing RpId."),
            RpName = rpName ?? throw new JsonException("FIDO2 credential is missing RpName."),
            UserHandle = userHandle ?? throw new JsonException("FIDO2 credential is missing UserHandle."),
            UserName = userName ?? throw new JsonException("FIDO2 credential is missing UserName."),
            UserDisplayName = userDisplayName ?? throw new JsonException("FIDO2 credential is missing UserDisplayName."),
            Counter = counter ?? throw new JsonException("FIDO2 credential is missing Counter."),
            Discoverable = discoverable ?? throw new JsonException("FIDO2 credential is missing Discoverable."),
            CreationDate = creationDate ?? throw new JsonException("FIDO2 credential is missing CreationDate.")
        };
    }

    private static TotpValue? ReadTotpCredential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return reader.ParseEncryptedValue(
            decryptKey,
            static value =>
            {
                TotpValue.TryParse(value, out var totpValue);
                return totpValue;
            });
    }
}
