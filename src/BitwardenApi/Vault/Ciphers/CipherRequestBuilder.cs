using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Cryptography.Keys;
using BitwardenApi.Vault.Ciphers.Models;
using BitwardenApi.Vault.Ciphers.Requests;
using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Vault.Ciphers.Serialization;

namespace BitwardenApi.Vault.Ciphers;

public static class CipherRequestBuilder
{
    public static CipherRequest Build(UnlockedUserKey userKey, Cipher cipher)
    {
        if (!cipher.Id.IsEmpty)
            throw new InvalidOperationException("CipherRequestBuilder builds new ciphers only.");

        Span<byte> cipherKey = stackalloc byte[64];
        RandomNumberGenerator.Fill(cipherKey);
        try
        {
            EncString wrappedKey = EncString.Encrypt(cipherKey, userKey.Key);
            return AssembleRequest(userKey, cipher, cipherKey, wrappedKey, null, null);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    internal static CipherRequest AssembleRequest(
        UnlockedUserKey userKey,
        Cipher cipher,
        ReadOnlySpan<byte> cipherKey,
        EncString wrappedKey,
        CipherDataPayload? originalData,
        CipherResponse? original,
        List<EncryptedCipherField>? fields = null,
        List<EncryptedPasswordHistory>? passwordHistory = null,
        List<EncryptedLoginUri>? selectedUris = null,
        EncString selectedTotp = default,
        List<EncryptedFido2Credential>? selectedCredentials = null)
        => new()
        {
            Type = cipher.Type,
            EncryptedFor = userKey.UserId.Value,
            FolderId = cipher.FolderId,
            Favorite = cipher.Favorite,
            Reprompt = cipher.Reprompt ? 1 : 0,
            Key = wrappedKey,
            Name = EncString.Encrypt(cipher.Name, cipherKey),
            Notes = EncString.Encrypt(cipher.Notes, cipherKey),
            Fields = fields,
            PasswordHistory = passwordHistory,
            Login = cipher is LoginCipher login
                ? new CipherLoginRequest
                {
                    Username = EncString.Encrypt(login.Username, cipherKey),
                    Password = EncString.Encrypt(login.Password, cipherKey),
                    Totp = originalData is null
                        ? EncString.Encrypt(login.Totp?.ToStorageString(), cipherKey)
                        : selectedTotp,
                    PasswordRevisionDate = (originalData as LoginCipherDataPayload)?.PasswordRevisionDate,
                    AutofillOnPageLoad = (originalData as LoginCipherDataPayload)?.AutofillOnPageLoad,
                    Uris = originalData is null ? EncryptLoginUris(login.Uris, cipherKey) : selectedUris ?? [],
                    Fido2Credentials = originalData is null
                        ? login.Fido2Credential is { } credential ? [EncryptFido2Credential(credential, cipherKey)] : []
                        : selectedCredentials ?? []
                }
                : null,
            Card = cipher is CardCipher card
                ? new CipherCardRequest
                {
                    CardholderName = EncString.Encrypt(card.CardholderName, cipherKey),
                    Brand = EncString.Encrypt(card.Brand, cipherKey),
                    Number = EncString.Encrypt(card.Number, cipherKey),
                    ExpMonth = EncString.Encrypt(card.ExpMonth, cipherKey),
                    ExpYear = EncString.Encrypt(card.ExpYear, cipherKey),
                    Code = EncString.Encrypt(card.Code, cipherKey)
                }
                : null,
            Identity = cipher is IdentityCipher identity
                ? new CipherIdentityRequest
                {
                    Title = EncString.Encrypt(identity.Title, cipherKey),
                    FirstName = EncString.Encrypt(identity.FirstName, cipherKey),
                    MiddleName = EncString.Encrypt(identity.MiddleName, cipherKey),
                    LastName = EncString.Encrypt(identity.LastName, cipherKey),
                    Address1 = EncString.Encrypt(identity.Address1, cipherKey),
                    Address2 = EncString.Encrypt(identity.Address2, cipherKey),
                    Address3 = EncString.Encrypt(identity.Address3, cipherKey),
                    City = EncString.Encrypt(identity.City, cipherKey),
                    State = EncString.Encrypt(identity.State, cipherKey),
                    PostalCode = EncString.Encrypt(identity.PostalCode, cipherKey),
                    Country = EncString.Encrypt(identity.Country, cipherKey),
                    Company = EncString.Encrypt(identity.Company, cipherKey),
                    Email = EncString.Encrypt(identity.Email, cipherKey),
                    Phone = EncString.Encrypt(identity.Phone, cipherKey),
                    Ssn = EncString.Encrypt(identity.Ssn, cipherKey),
                    Username = EncString.Encrypt(identity.Username, cipherKey),
                    PassportNumber = EncString.Encrypt(identity.PassportNumber, cipherKey),
                    LicenseNumber = EncString.Encrypt(identity.LicenseNumber, cipherKey)
                }
                : null,
            SecureNote = cipher is SecureNoteCipher
                ? new CipherSecureNoteRequest { Type = (originalData as SecureNoteCipherDataPayload)?.Type ?? 0 }
                : null,
            SshKey = cipher is SshKeyCipher sshKey
                ? new CipherSshKeyRequest
                {
                    PrivateKey = EncString.Encrypt(sshKey.PrivateKey, cipherKey),
                    PublicKey = EncString.Encrypt(sshKey.PublicKey.RawKey, cipherKey),
                    KeyFingerprint = EncString.Encrypt(sshKey.KeyFingerprint, cipherKey)
                }
                : null,
            ArchivedDate = original?.ArchivedDate,
            LastKnownRevisionDate = original?.RevisionDate
        };

    internal static List<EncryptedLoginUri> EncryptLoginUris(List<LoginUri> uris, ReadOnlySpan<byte> cipherKey)
    {
        List<EncryptedLoginUri> encrypted = new(uris.Count);
        foreach (LoginUri uri in uris)
        {
            encrypted.Add(new EncryptedLoginUri
            {
                Uri = EncString.Encrypt(uri.Value, cipherKey),
                Match = (int)uri.Match
            });
        }

        return encrypted;
    }

    internal static EncryptedFido2Credential EncryptFido2Credential(Fido2Credential credential, ReadOnlySpan<byte> cipherKey)
        => new()
        {
            CredentialId = EncString.Encrypt(new Guid(credential.CredentialId, bigEndian: true).ToString(), cipherKey),
            KeyType = EncString.Encrypt(credential.KeyType.ToWireValue(), cipherKey),
            KeyAlgorithm = EncString.Encrypt(credential.KeyAlgorithm.ToWireValue(), cipherKey),
            KeyCurve = EncString.Encrypt(credential.KeyCurve.ToWireValue(), cipherKey),
            KeyValue = EncString.Encrypt(Base64Url.EncodeToString(credential.KeyValue), cipherKey),
            RpId = EncString.Encrypt(credential.RpId, cipherKey),
            RpName = EncString.Encrypt(credential.RpName, cipherKey),
            UserHandle = EncString.Encrypt(Base64Url.EncodeToString(credential.UserHandle), cipherKey),
            UserName = EncString.Encrypt(credential.UserName, cipherKey),
            UserDisplayName = EncString.Encrypt(credential.UserDisplayName, cipherKey),
            Counter = EncString.Encrypt(credential.Counter.ToString(CultureInfo.InvariantCulture), cipherKey),
            Discoverable = EncString.Encrypt(credential.Discoverable ? "true" : "false", cipherKey),
            CreationDate = credential.CreationDate
        };
}
