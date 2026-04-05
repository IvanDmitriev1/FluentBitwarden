using BitwardenApi.Cryptography;
using BitwardenApi.Cryptography.Enc;
using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    public static Cipher ParseAndDecryptCipher(in CipherDto dto, DecryptedUserKey decryptedUserKey)
    {
        return dto.CipherType switch
        {
            CipherType.Login => ParseLogin(dto, decryptedUserKey),
            CipherType.SecureNote => ParseSecureNote(dto, decryptedUserKey),
            CipherType.Card => ParseCard(dto, decryptedUserKey),
            CipherType.Identity => ParseIdentity(dto, decryptedUserKey),
            _ => throw new NotSupportedException($"Unsupported cipher type: {dto.CipherType}")
        };
    }

    private static Cipher ParseLogin(
        in CipherDto dto,
        DecryptedUserKey key)
    {
        var reader = new Utf8JsonReader(dto.Payload, true, default);
        var cipher = new LoginCipher { Id = dto.Id, FolderId = dto.FolderId, Name = string.Empty, Uris = [] };

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("Username"u8))
                cipher.Username = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Password"u8))
                cipher.Password = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Totp"u8))
                cipher.Totp = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Uris"u8))
                cipher.Uris = ReadUris(ref reader, key);
            else
                SkipValue(ref reader);
        }

        return cipher;
    }

    private static Cipher ParseSecureNote(in CipherDto dto, DecryptedUserKey decryptedUserKey)
    {
        var reader = new Utf8JsonReader(dto.Payload, true, default);
        var cipher = new SecureNoteCipher { Id = dto.Id, FolderId = dto.FolderId, Name = string.Empty };

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) 
                continue;
        }

        return cipher;
    }

    private static Cipher ParseCard(in CipherDto dto, DecryptedUserKey key)
    {
        var reader = new Utf8JsonReader(dto.Payload, true, default);
        var cipher = new CardCipher { Id = dto.Id, FolderId = dto.FolderId, Name = string.Empty };

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("CardholderName"u8))
                cipher.CardholderName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Brand"u8))
                cipher.Brand = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Number"u8))
                cipher.Number = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("ExpMonth"u8))
                cipher.ExpMonth = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("ExpYear"u8))
                cipher.ExpYear = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Code"u8))
                cipher.Code = ReadDecryptField(ref reader, key);
            else
                SkipValue(ref reader);
        }

        return cipher;
    }

    private static Cipher ParseIdentity(in CipherDto dto, DecryptedUserKey key)
    {
        var reader = new Utf8JsonReader(dto.Payload, true, default);
        var cipher = new IdentityCipher { Id = dto.Id, FolderId = dto.FolderId, Name = string.Empty };

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) 
                continue;

            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("Title"u8)) cipher.Title = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("FirstName"u8)) cipher.FirstName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("MiddleName"u8)) cipher.MiddleName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("LastName"u8)) cipher.LastName = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Address1"u8)) cipher.Address1 = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Address2"u8)) cipher.Address2 = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Address3"u8)) cipher.Address3 = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("City"u8)) cipher.City = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("State"u8)) cipher.State = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("PostalCode"u8)) cipher.PostalCode = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Country"u8)) cipher.Country = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Company"u8)) cipher.Company = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Email"u8)) cipher.Email = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Phone"u8)) cipher.Phone = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Ssn"u8)) cipher.Ssn = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("Username"u8)) cipher.Username = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("PassportNumber"u8)) cipher.PassportNumber = ReadDecryptField(ref reader, key);
            else if (reader.ValueTextEquals("LicenseNumber"u8)) cipher.LicenseNumber = ReadDecryptField(ref reader, key);
            else SkipValue(ref reader);
        }

        return cipher;
    }

    private static void SkipValue(ref Utf8JsonReader reader)
    {
        reader.Read();

        // Skip() fast-forwards to EndObject/EndArray; on primitives it's a no-op.
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            reader.Skip();
    }

    private static string? ReadDecryptField(ref Utf8JsonReader reader, DecryptedUserKey key)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var encString = EncString.From(reader.ValueSpan);
        var parts = encString.Parse();

        return CryptographyService.DecryptString(parts, key);
    }

    private static List<string> ReadUris(ref Utf8JsonReader reader, DecryptedUserKey key)
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

                if (reader.ValueTextEquals("Uri"u8))
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

        return [.. uris];
    }

}