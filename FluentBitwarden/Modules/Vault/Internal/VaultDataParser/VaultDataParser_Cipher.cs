using System.Text.Json;
using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.OpenSsh;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    public static VaultCipher ParseAndDecryptCipher(ref readonly VaultCipherDto dto, ReadOnlySpan<byte> payload, DecryptedUserKey decryptedUserKey)
    {
        var cipher = CreateCipher(in dto);
        var reader = CreateObjectReader(payload);

        Span<byte> keyBuffer = dto.EncryptedKey is null
            ? Span<byte>.Empty
            : stackalloc byte[64];

        if (dto.EncryptedKey is not null)
        {
            CryptographyService.UnwrapSymmetricKey(dto.EncryptedKey, decryptedUserKey, keyBuffer);
        }

        ReadOnlySpan<byte> decryptionKey = dto.EncryptedKey is null
            ? decryptedUserKey.Key
            : keyBuffer;

        return dto.CipherType switch
        {
            CipherType.Login => ParseLoginCipher((LoginVaultCipher)cipher, ref reader, decryptionKey),
            CipherType.SecureNote => ParseSecureNoteCipher((SecureNoteVaultCipher)cipher, ref reader, decryptionKey),
            CipherType.Card => ParseCardCipher((CardVaultCipher)cipher, ref reader, decryptionKey),
            CipherType.Identity => ParseIdentityCipher((IdentityVaultCipher)cipher, ref reader, decryptionKey),
            CipherType.SshKey => ParseSshKeyCipher((SshKeyVaultCipher)cipher, ref reader, decryptionKey),
            _ => throw new NotSupportedException($"Unsupported vaultCipher type: {dto.CipherType}")
        };
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
                    c.Fido2Credentials = ReadJsonArray(ref r, k, ReadFido2Credential);
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

    private static VaultCipher CreateCipher(ref readonly VaultCipherDto dto) => dto.CipherType switch
    {
        CipherType.Login => new LoginVaultCipher
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
        CipherType.SecureNote => new SecureNoteVaultCipher()
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
        CipherType.Card => new CardVaultCipher()
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
        CipherType.Identity => new IdentityVaultCipher()
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
        CipherType.SshKey => new SshKeyVaultCipher()
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

    private static string ReadUri(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return string.Empty;

        string? uri = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("uri"u8) || reader.ValueTextEquals("Uri"u8))
                uri = ReadDecryptField(ref reader, decryptKey);
            else
                SkipValue(ref reader);
        }

        ArgumentNullException.ThrowIfNull(uri);
        return uri;
    }

    private static Fido2Credential ReadFido2Credential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> decryptKey)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Each Fido2Credentials item must be a JSON object.");

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
                keyValue = ReadBase64UrlBytes(ref reader, decryptKey, "KeyValue");
            else if (reader.ValueTextEquals("rpId"u8) || reader.ValueTextEquals("RpId"u8))
                rpId = ReadRequiredDecryptField(ref reader, decryptKey, "RpId");
            else if (reader.ValueTextEquals("rpName"u8) || reader.ValueTextEquals("RpName"u8))
                rpName = ReadRequiredDecryptField(ref reader, decryptKey, "RpName");
            else if (reader.ValueTextEquals("userHandle"u8) || reader.ValueTextEquals("UserHandle"u8))
                userHandle = ReadBase64UrlBytes(ref reader, decryptKey, "UserHandle");
            else if (reader.ValueTextEquals("userName"u8) || reader.ValueTextEquals("UserName"u8))
                userName = ReadRequiredDecryptField(ref reader, decryptKey, "UserName");
            else if (reader.ValueTextEquals("userDisplayName"u8) || reader.ValueTextEquals("UserDisplayName"u8))
                userDisplayName = ReadRequiredDecryptField(ref reader, decryptKey, "UserDisplayName");
            else if (reader.ValueTextEquals("counter"u8) || reader.ValueTextEquals("Counter"u8))
                counter = ReadRequiredEncryptedInt32(ref reader, decryptKey, "Counter");
            else if (reader.ValueTextEquals("discoverable"u8) || reader.ValueTextEquals("Discoverable"u8))
                discoverable = ReadRequiredEncryptedBoolean(ref reader, decryptKey, "Discoverable");
            else if (reader.ValueTextEquals("creationDate"u8) || reader.ValueTextEquals("CreationDate"u8))
                creationDate = ReadRequiredDateTimeOffset(ref reader, "CreationDate");
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

        Span<byte> decodedSpan = stackalloc byte[reader.ValueSpan.Length];
        int bytesWritten = CryptographyService.DecryptStringTo(ref reader, decryptKey, decodedSpan);
        Span<byte> decodedValue = decodedSpan[..bytesWritten];

        TotpValue.TryParse(decodedValue, out var totpValue);
        return totpValue;
    }
}
