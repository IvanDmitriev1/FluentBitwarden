using System.Buffers.Text;
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

    private static LoginCipher ParseLoginCipher(LoginCipher cipher, ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key)
        => ParseCipherObject(cipher, ref reader, key,
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


    private static SecureNoteCipher ParseSecureNoteCipher(SecureNoteCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
        => ParseCipherObject(cipher, ref reader, key, static (ref _, _, scoped _) => false);

    private static CardCipher ParseCardCipher(CardCipher cipher, ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
        => ParseCipherObject(cipher, ref reader, key, static (ref r, c, scoped k) =>
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

    private static IdentityCipher ParseIdentityCipher(IdentityCipher cipher, ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key)
        => ParseCipherObject(cipher, ref reader, key,
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

    private static SshKeyCipher ParseSshKeyCipher(SshKeyCipher cipher, ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key)
        => ParseCipherObject(cipher, ref reader, key,
            static (ref r, c, scoped k) =>
            {
                if (r.ValueTextEquals("privateKey"u8) || r.ValueTextEquals("PrivateKey"u8))
                    c.PrivateKey = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("publicKey"u8) || r.ValueTextEquals("PublicKey"u8))
                    c.PublicKey = ReadDecryptField(ref r, k);
                else if (r.ValueTextEquals("keyFingerprint"u8) || r.ValueTextEquals("KeyFingerprint"u8))
                    c.KeyFingerprint = ReadDecryptField(ref r, k);
                else
                    return false;
                return true;
            });

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

    private static string ReadUri(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
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
                uri = ReadDecryptField(ref reader, key);
            else
                SkipValue(ref reader);
        }

        ArgumentNullException.ThrowIfNull(uri);
        return uri;
    }

    private static Fido2Credential ReadFido2Credential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
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
                credentialId = Fido2CredentialJsonMapper.ParseCredentialId(ReadRequiredDecryptField(ref reader, key, "CredentialId"));
            else if (reader.ValueTextEquals("keyType"u8) || reader.ValueTextEquals("KeyType"u8))
                keyType = Fido2CredentialJsonMapper.ParseKeyType(ReadRequiredDecryptField(ref reader, key, "KeyType"));
            else if (reader.ValueTextEquals("keyAlgorithm"u8) || reader.ValueTextEquals("KeyAlgorithm"u8))
                keyAlgorithm = Fido2CredentialJsonMapper.ParseKeyAlgorithm(ReadRequiredDecryptField(ref reader, key, "KeyAlgorithm"));
            else if (reader.ValueTextEquals("keyCurve"u8) || reader.ValueTextEquals("KeyCurve"u8))
                keyCurve = Fido2CredentialJsonMapper.ParseKeyCurve(ReadRequiredDecryptField(ref reader, key, "KeyCurve"));
            else if (reader.ValueTextEquals("keyValue"u8) || reader.ValueTextEquals("KeyValue"u8))
                keyValue = Base64Url.DecodeFromChars(ReadRequiredDecryptField(ref reader, key, "KeyValue"));
            else if (reader.ValueTextEquals("rpId"u8) || reader.ValueTextEquals("RpId"u8))
                rpId = ReadRequiredDecryptField(ref reader, key, "RpId");
            else if (reader.ValueTextEquals("rpName"u8) || reader.ValueTextEquals("RpName"u8))
                rpName = ReadRequiredDecryptField(ref reader, key, "RpName");
            else if (reader.ValueTextEquals("userHandle"u8) || reader.ValueTextEquals("UserHandle"u8))
                userHandle = Base64Url.DecodeFromChars(ReadRequiredDecryptField(ref reader, key, "UserHandle"));
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

    private static TotpValue? ReadTotpCredential(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        Span<byte> decodedSpan = stackalloc byte[reader.ValueSpan.Length];
        int bytesWritten = CryptographyService.DecryptStringTo(ref reader, key, decodedSpan);
        Span<byte> decodedValue = decodedSpan[..bytesWritten];

        TotpValue.TryParse(decodedValue, out var totpValue);
        return totpValue;
    }
}
