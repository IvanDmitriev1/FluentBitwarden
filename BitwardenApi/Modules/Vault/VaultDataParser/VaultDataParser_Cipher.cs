using System.Diagnostics;
using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    public static Cipher ParseAndDecryptCipher(in CipherDto dto, ReadOnlySpan<byte> payload, DecryptedUserKey decryptedUserKey)
    {
        var cipher = CreateCipher(dto);
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
                cipher.Username = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("password"u8) || reader.ValueTextEquals("Password"u8))
                cipher.Password = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("totp"u8) || reader.ValueTextEquals("Totp"u8))
                cipher.Totp = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("uris"u8) || reader.ValueTextEquals("Uris"u8))
                cipher.Uris = ReadUris(ref reader, key);
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

    private static Cipher CreateCipher(in CipherDto dto)
    {
        return dto.CipherType switch
        {
            CipherType.Login => new LoginCipher()
            {
                Id = dto.Id,
                FolderId = dto.FolderId,
                Name = string.Empty,
                Favorite = dto.Favorite,
                Reprompt = dto.Reprompt,
                RevisionDate = dto.RevisionDate,
                CreationDate = dto.CreationDate,
                DeletedDate = dto.DeletedDate,
                Uris = []
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
    }

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
}
