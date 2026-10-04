using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;

namespace BitwardenApi.Vault.Items;

public static class VaultCipherRequestFactory
{
    public static VaultCipherRequest BuildRequest(UnlockedUserKey userKey, VaultCipher cipher)
    {
        const int cipherKeyByteLength = 64;
        Span<byte> cipherKey = stackalloc byte[cipherKeyByteLength];
        RandomNumberGenerator.Fill(cipherKey);

        var wrappedKey = EncString.Encrypt(cipherKey, userKey.Key);
        return new VaultCipherRequest
        {
            Type = cipher.Type,
            EncryptedFor = userKey.UserId.Value,
            FolderId = cipher.FolderId.IsEmpty ? null : cipher.FolderId.ToString(),
            Favorite = cipher.Favorite,
            Reprompt = cipher.Reprompt ? 1 : 0,
            Key = wrappedKey,
            Name = Encrypt(cipher.Name, cipherKey),
            Notes = Encrypt(cipher.Notes, cipherKey),
            Login = cipher is LoginVaultCipher login ? BuildLogin(login, cipherKey) : null,
            Card = cipher is CardVaultCipher card ? BuildCard(card, cipherKey) : null,
            Identity = cipher is IdentityVaultCipher identity ? BuildIdentity(identity, cipherKey) : null,
            SecureNote = cipher is SecureNoteVaultCipher ? new CipherSecureNoteRequest { Type = 0 } : null,
            SshKey = cipher is SshKeyVaultCipher sshKey ? BuildSshKey(sshKey, cipherKey) : null,
            LastKnownRevisionDate = cipher.Id.IsEmpty ? null : cipher.RevisionDate.UtcDateTime
        };
    }

    private static CipherLoginRequest BuildLogin(LoginVaultCipher login, ReadOnlySpan<byte> key)
    {
        var uris = new List<CipherLoginUriRequest>(login.Uris.Count);
        foreach (var uri in login.Uris)
        {
            uris.Add(new CipherLoginUriRequest
            {
                Uri = Encrypt(uri.Value, key),
                Match = (int)uri.Match
            });
        }

        var fido2Credentials = login.Fido2Credential is { } credential
            ? new List<CipherFido2CredentialRequest> { BuildFido2Credential(credential, key) }
            : [];

        return new CipherLoginRequest
        {
            Username = Encrypt(login.Username, key),
            Password = Encrypt(login.Password, key),
            Totp = login.Totp is { } totp ? Encrypt(totp.ToStorageString(), key) : EncString.Empty,
            Uris = uris,
            Fido2Credentials = fido2Credentials
        };
    }

    private static CipherFido2CredentialRequest BuildFido2Credential(Fido2Credential credential, ReadOnlySpan<byte> key)
        => new()
        {
            CredentialId = Encrypt(new Guid(credential.CredentialId, bigEndian: true).ToString(), key),
            KeyType = Encrypt(credential.KeyType.ToWireValue(), key),
            KeyAlgorithm = Encrypt(credential.KeyAlgorithm.ToWireValue(), key),
            KeyCurve = Encrypt(credential.KeyCurve.ToWireValue(), key),
            KeyValue = Encrypt(Base64Url.EncodeToString(credential.KeyValue), key),
            RpId = Encrypt(credential.RpId, key),
            RpName = Encrypt(credential.RpName, key),
            UserHandle = Encrypt(Base64Url.EncodeToString(credential.UserHandle), key),
            UserName = Encrypt(credential.UserName, key),
            UserDisplayName = Encrypt(credential.UserDisplayName, key),
            Counter = Encrypt(credential.Counter.ToString(CultureInfo.InvariantCulture), key),
            Discoverable = Encrypt(credential.Discoverable ? "true" : "false", key),
            CreationDate = credential.CreationDate.UtcDateTime
        };

    private static CipherCardRequest BuildCard(CardVaultCipher card, ReadOnlySpan<byte> key)
        => new()
        {
            CardholderName = Encrypt(card.CardholderName, key),
            Brand = Encrypt(card.Brand, key),
            Number = Encrypt(card.Number, key),
            ExpMonth = Encrypt(card.ExpMonth, key),
            ExpYear = Encrypt(card.ExpYear, key),
            Code = Encrypt(card.Code, key)
        };

    private static CipherIdentityRequest BuildIdentity(IdentityVaultCipher identity, ReadOnlySpan<byte> key)
        => new()
        {
            Title = Encrypt(identity.Title, key),
            FirstName = Encrypt(identity.FirstName, key),
            MiddleName = Encrypt(identity.MiddleName, key),
            LastName = Encrypt(identity.LastName, key),
            Address1 = Encrypt(identity.Address1, key),
            Address2 = Encrypt(identity.Address2, key),
            Address3 = Encrypt(identity.Address3, key),
            City = Encrypt(identity.City, key),
            State = Encrypt(identity.State, key),
            PostalCode = Encrypt(identity.PostalCode, key),
            Country = Encrypt(identity.Country, key),
            Company = Encrypt(identity.Company, key),
            Email = Encrypt(identity.Email, key),
            Phone = Encrypt(identity.Phone, key),
            Ssn = Encrypt(identity.Ssn, key),
            Username = Encrypt(identity.Username, key),
            PassportNumber = Encrypt(identity.PassportNumber, key),
            LicenseNumber = Encrypt(identity.LicenseNumber, key)
        };

    private static CipherSshKeyRequest BuildSshKey(SshKeyVaultCipher sshKey, ReadOnlySpan<byte> key)
        => new()
        {
            PrivateKey = Encrypt(sshKey.PrivateKey, key),
            PublicKey = Encrypt(sshKey.PublicKey.RawKey, key),
            KeyFingerprint = Encrypt(sshKey.KeyFingerprint, key)
        };

    private static EncString Encrypt(string? plaintext, ReadOnlySpan<byte> key)
        => string.IsNullOrEmpty(plaintext) ? EncString.Empty : EncString.Encrypt(plaintext, key);
}
