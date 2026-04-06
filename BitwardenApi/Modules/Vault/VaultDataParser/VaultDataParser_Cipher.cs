using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Vault.Internal;
using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    public static Cipher ParseAndDecryptCipher(ref readonly CipherDto dto, ReadOnlySpan<byte> payload, DecryptedUserKey decryptedUserKey)
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
            CipherType.Login => ParseLoginCipher((LoginCipher)cipher, ref reader, decryptionKey),
            CipherType.SecureNote => ParseSecureNoteCipher((SecureNoteCipher)cipher, ref reader, decryptionKey),
            CipherType.Card => ParseCardCipher((CardCipher)cipher, ref reader, decryptionKey),
            CipherType.Identity => ParseIdentityCipher((IdentityCipher)cipher, ref reader, decryptionKey),
            CipherType.SshKey => ParseSshKeyCipher((SshKeyCipher)cipher, ref reader, decryptionKey),
            _ => throw new NotSupportedException($"Unsupported cipher type: {dto.CipherType}")
        };
    }

    private static LoginCipher ParseLoginCipher(LoginCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (TryReadCommonCipherProperty(ref reader, cipher, key))
            {
                continue;
            }

            if (reader.ValueTextEquals("username"u8) || reader.ValueTextEquals("Username"u8))
                cipher.Username = ReadRequiredDecryptField(ref reader, key, "Username");
            else if (reader.ValueTextEquals("password"u8) || reader.ValueTextEquals("Password"u8))
                cipher.Password = ReadRequiredDecryptField(ref reader, key, "Password");
            else if (reader.ValueTextEquals("totp"u8) || reader.ValueTextEquals("Totp"u8))
                cipher.Totp = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("uris"u8) || reader.ValueTextEquals("Uris"u8))
                cipher.Uris = ReadUris(ref reader, key);
            else if (reader.ValueTextEquals("fido2Credentials"u8) || reader.ValueTextEquals("Fido2Credentials"u8))
                cipher.Fido2Credentials = ReadFido2Credentials(ref reader, key);
            else
                SkipValue(ref reader);
        }

        return cipher;
    }

    private static SecureNoteCipher ParseSecureNoteCipher(SecureNoteCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (TryReadCommonCipherProperty(ref reader, cipher, key))
            {
                continue;
            }

            SkipValue(ref reader);
        }

        return cipher;
    }

    private static CardCipher ParseCardCipher(CardCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (TryReadCommonCipherProperty(ref reader, cipher, key))
                continue;

            if (reader.ValueTextEquals("cardholderName"u8) || reader.ValueTextEquals("CardholderName"u8))
                cipher.CardholderName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("brand"u8) || reader.ValueTextEquals("Brand"u8))
                cipher.Brand = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("number"u8) || reader.ValueTextEquals("Number"u8))
                cipher.Number = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("expMonth"u8) || reader.ValueTextEquals("ExpMonth"u8))
                cipher.ExpMonth = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("expYear"u8) || reader.ValueTextEquals("ExpYear"u8))
                cipher.ExpYear = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("code"u8) || reader.ValueTextEquals("Code"u8))
                cipher.Code = ReadDecryptField(ref reader, key);
            else
                SkipValue(ref reader);
        }

        return cipher;
    }

    private static IdentityCipher ParseIdentityCipher(IdentityCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (TryReadCommonCipherProperty(ref reader, cipher, key))
                continue;

            if (reader.ValueTextEquals("title"u8) || reader.ValueTextEquals("Title"u8))
                cipher.Title = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("firstName"u8) || reader.ValueTextEquals("FirstName"u8))
                cipher.FirstName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("middleName"u8) || reader.ValueTextEquals("MiddleName"u8))
                cipher.MiddleName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("lastName"u8) || reader.ValueTextEquals("LastName"u8))
                cipher.LastName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("address1"u8) || reader.ValueTextEquals("Address1"u8))
                cipher.Address1 = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("address2"u8) || reader.ValueTextEquals("Address2"u8))
                cipher.Address2 = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("address3"u8) || reader.ValueTextEquals("Address3"u8))
                cipher.Address3 = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("city"u8) || reader.ValueTextEquals("City"u8))
                cipher.City = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("state"u8) || reader.ValueTextEquals("State"u8))
                cipher.State = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("postalCode"u8) || reader.ValueTextEquals("PostalCode"u8))
                cipher.PostalCode = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("country"u8) || reader.ValueTextEquals("Country"u8))
                cipher.Country = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("company"u8) || reader.ValueTextEquals("Company"u8))
                cipher.Company = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("email"u8) || reader.ValueTextEquals("Email"u8))
                cipher.Email = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("phone"u8) || reader.ValueTextEquals("Phone"u8))
                cipher.Phone = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("ssn"u8) || reader.ValueTextEquals("Ssn"u8))
                cipher.Ssn = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("username"u8) || reader.ValueTextEquals("Username"u8))
                cipher.Username = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("passportNumber"u8) || reader.ValueTextEquals("PassportNumber"u8))
                cipher.PassportNumber = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("licenseNumber"u8) || reader.ValueTextEquals("LicenseNumber"u8))
                cipher.LicenseNumber = ReadDecryptField(ref reader, key);
            else SkipValue(ref reader);
        }

        return cipher;
    }

    private static SshKeyCipher ParseSshKeyCipher(SshKeyCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (TryReadCommonCipherProperty(ref reader, cipher, key))
                continue;

            if (reader.ValueTextEquals("privateKey"u8) || reader.ValueTextEquals("PrivateKey"u8))
                cipher.PrivateKey = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("publicKey"u8) || reader.ValueTextEquals("PublicKey"u8))
                cipher.PublicKey = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("keyFingerprint"u8) || reader.ValueTextEquals("KeyFingerprint"u8))
                cipher.KeyFingerprint = ReadDecryptField(ref reader, key);
            else SkipValue(ref reader);
        }

        return cipher;
    }

    private static bool TryReadCommonCipherProperty(ref Utf8JsonReader reader, Cipher cipher, scoped ReadOnlySpan<byte> key)
    {
        if (reader.ValueTextEquals("name"u8) || reader.ValueTextEquals("Name"u8))
        {
            cipher.Name = ReadDecryptField(ref reader, key) ?? string.Empty;
            return true;
        }

        if (reader.ValueTextEquals("notes"u8) || reader.ValueTextEquals("Notes"u8))
        {
            cipher.Notes = ReadDecryptField(ref reader, key);
            return true;
        }

        return false;
    }

    private static Cipher CreateCipher(ref readonly CipherDto dto) => dto.CipherType switch
    {
        CipherType.Login => new LoginCipher
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
        CipherType.SecureNote => new SecureNoteCipher()
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
        CipherType.Card => new CardCipher()
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
        CipherType.Identity => new IdentityCipher()
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
        CipherType.SshKey => new SshKeyCipher()
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

    private static List<string> ReadUris(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartArray)
            return [];

        var uris = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartObject)
                continue;

            string? uri = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                if (reader.ValueTextEquals("uri"u8) || reader.ValueTextEquals("Uri"u8))
                {
                    uri = ReadDecryptField(ref reader, key);
                }
                else
                {
                    SkipValue(ref reader);
                }
            }

            ArgumentNullException.ThrowIfNull(uri);
            uris.Add(uri);
        }

        return uris;
    }

    private static List<Fido2Credential> ReadFido2Credentials(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Fido2Credentials must be a JSON array.");
        }

        var credentials = new List<Fido2Credential>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Each Fido2Credentials item must be a JSON object.");
            }

            credentials.Add(ReadFido2Credential(ref reader, key));
        }

        return credentials;
    }

    private static Fido2Credential ReadFido2Credential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        string? credentialId = null;
        Fido2CredentialKeyType? keyType = null;
        Fido2CredentialKeyAlgorithm? keyAlgorithm = null;
        Fido2CredentialKeyCurve? keyCurve = null;
        string? keyValue = null;
        string? rpId = null;
        string? rpName = null;
        string? userHandle = null;
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
                credentialId = ReadRequiredDecryptField(ref reader, key, "CredentialId");
            else if (reader.ValueTextEquals("keyType"u8) || reader.ValueTextEquals("KeyType"u8))
                keyType = Fido2CredentialJsonMapper.ParseKeyType(ReadRequiredDecryptField(ref reader, key, "KeyType"));
            else if (reader.ValueTextEquals("keyAlgorithm"u8) || reader.ValueTextEquals("KeyAlgorithm"u8))
                keyAlgorithm = Fido2CredentialJsonMapper.ParseKeyAlgorithm(ReadRequiredDecryptField(ref reader, key, "KeyAlgorithm"));
            else if (reader.ValueTextEquals("keyCurve"u8) || reader.ValueTextEquals("KeyCurve"u8))
                keyCurve = Fido2CredentialJsonMapper.ParseKeyCurve(ReadRequiredDecryptField(ref reader, key, "KeyCurve"));
            else if (reader.ValueTextEquals("keyValue"u8) || reader.ValueTextEquals("KeyValue"u8))
                keyValue = ReadRequiredDecryptField(ref reader, key, "KeyValue");
            else if (reader.ValueTextEquals("rpId"u8) || reader.ValueTextEquals("RpId"u8))
                rpId = ReadRequiredDecryptField(ref reader, key, "RpId");
            else if (reader.ValueTextEquals("rpName"u8) || reader.ValueTextEquals("RpName"u8))
                rpName = ReadRequiredDecryptField(ref reader, key, "RpName");
            else if (reader.ValueTextEquals("userHandle"u8) || reader.ValueTextEquals("UserHandle"u8))
                userHandle = ReadRequiredDecryptField(ref reader, key, "UserHandle");
            else if (reader.ValueTextEquals("userName"u8) || reader.ValueTextEquals("UserName"u8))
                userName = ReadRequiredDecryptField(ref reader, key, "UserName");
            else if (reader.ValueTextEquals("userDisplayName"u8) || reader.ValueTextEquals("UserDisplayName"u8))
                userDisplayName = ReadRequiredDecryptField(ref reader, key, "UserDisplayName");
            else if (reader.ValueTextEquals("counter"u8) || reader.ValueTextEquals("Counter"u8))
                counter = ReadRequiredEncryptedInt32(ref reader, key, "Counter");
            else if (reader.ValueTextEquals("discoverable"u8) || reader.ValueTextEquals("Discoverable"u8))
                discoverable = ReadRequiredEncryptedBoolean(ref reader, key, "Discoverable");
            else if (reader.ValueTextEquals("creationDate"u8) || reader.ValueTextEquals("CreationDate"u8))
                creationDate = ReadRequiredDateTimeOffset(ref reader, "CreationDate");
            else
                SkipValue(ref reader);
        }

        return new Fido2Credential
        {
            CredentialId = credentialId!,
            KeyType = keyType!.Value,
            KeyAlgorithm = keyAlgorithm!.Value,
            KeyCurve = keyCurve!.Value,
            KeyValue = keyValue!,
            RpId = rpId!,
            RpName = rpName!,
            UserHandle = userHandle!,
            UserName = userName!,
            UserDisplayName = userDisplayName!,
            Counter = counter!.Value,
            Discoverable = discoverable!.Value,
            CreationDate = creationDate!.Value
        };
    }
}
