using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Cryptography.Keys;
using BitwardenApi.Vault.Ciphers.Models;
using BitwardenApi.Vault.Ciphers.Requests;
using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Vault.Ciphers.Serialization;

namespace BitwardenApi.Vault.Ciphers;

public static class CipherUpdateRequestBuilder
{
    public static CipherRequest Build(UnlockedUserKey userKey, Cipher editedCipher, in CipherResponse original)
    {
        if (editedCipher.Id.IsEmpty || original.Id.IsEmpty || editedCipher.Id != original.Id)
            throw new InvalidOperationException("An update must keep the original non-empty cipher id.");
        if (editedCipher.Type != original.VaultCipherType)
            throw new InvalidOperationException("An update cannot change the cipher type.");
        if (editedCipher.RevisionDate != original.RevisionDate)
            throw new InvalidOperationException("The cipher changed since it was opened. Sync before saving.");
        if (!original.OrganizationId.IsEmpty)
            throw new NotSupportedException("Editing organization ciphers is not supported.");

        CipherDataPayload originalData = DeserializeData(original);
        ValidateCredentialFields(originalData);
        Cipher originalProjection = new CipherDataReader().Read(in original, original.Data, userKey);
        using var cipherKey = CipherKey.Create(original.ProtectedCipherKey, userKey);

        List<EncryptedLoginUri>? uris = null;
        EncString totp = default;
        List<EncryptedFido2Credential>? credentials = null;
        if (editedCipher is LoginCipher editedLogin)
        {
            var savedLogin = (LoginCipherDataPayload)originalData;
            var projectedLogin = (LoginCipher)originalProjection;
            uris = BuildUpdatedUris(editedLogin, projectedLogin.Uris, savedLogin, cipherKey.Key);
            totp = editedLogin.Totp is null && projectedLogin.Totp is null
                ? savedLogin.Totp
                : EncString.Encrypt(editedLogin.Totp?.ToStorageString(), cipherKey.Key);
            credentials = BuildUpdatedCredentials(
                editedLogin.Fido2Credential,
                projectedLogin.Fido2Credential,
                savedLogin.Fido2Credentials,
                cipherKey.Key);
        }

        return CipherRequestBuilder.AssembleRequest(
            userKey,
            editedCipher,
            cipherKey.Key,
            original.ProtectedCipherKey,
            originalData,
            original,
            originalData.Fields,
            originalData.PasswordHistory,
            uris,
            totp,
            credentials);
    }

    private static CipherDataPayload DeserializeData(CipherResponse original)
        => original.VaultCipherType switch
        {
            CipherType.Login => JsonSerializer.Deserialize(original.Data, CipherDataPayloadJsonContext.Default.LoginCipherDataPayload)
                ?? throw new JsonException("Login cipher data cannot be null."),
            CipherType.SecureNote => JsonSerializer.Deserialize(original.Data, CipherDataPayloadJsonContext.Default.SecureNoteCipherDataPayload)
                ?? throw new JsonException("Secure note data cannot be null."),
            CipherType.Card => JsonSerializer.Deserialize(original.Data, CipherDataPayloadJsonContext.Default.CardCipherDataPayload)
                ?? throw new JsonException("Card data cannot be null."),
            CipherType.Identity => JsonSerializer.Deserialize(original.Data, CipherDataPayloadJsonContext.Default.IdentityCipherDataPayload)
                ?? throw new JsonException("Identity data cannot be null."),
            CipherType.SshKey => JsonSerializer.Deserialize(original.Data, CipherDataPayloadJsonContext.Default.SshKeyCipherDataPayload)
                ?? throw new JsonException("SSH key data cannot be null."),
            _ => throw new NotSupportedException($"Cipher type {original.VaultCipherType} cannot be updated.")
        };

    private static List<EncryptedLoginUri> BuildUpdatedUris(
        LoginCipher edited,
        List<LoginUri> projected,
        LoginCipherDataPayload original,
        ReadOnlySpan<byte> cipherKey)
    {
        bool unchanged = edited.Uris.Count == projected.Count;
        for (int i = 0; unchanged && i < edited.Uris.Count; i++)
        {
            unchanged = string.Equals(edited.Uris[i].Value, projected[i].Value, StringComparison.Ordinal) &&
                edited.Uris[i].Match == projected[i].Match;
        }

        if (unchanged)
        {
            if (original.Uris is { Count: > 0 })
                return original.Uris;
            return original.Uri.IsEmpty ? [] : [new EncryptedLoginUri { Uri = original.Uri }];
        }

        if (original.Uris?.Any(static uri => !string.IsNullOrEmpty(uri.UriChecksum) || uri.Match is null) == true)
        {
            throw new InvalidOperationException("This cipher has URI metadata that the editor cannot preserve.");
        }

        if (!original.Uri.IsEmpty &&
            (original.Uris is not { Count: > 0 } ||
             !string.Equals(original.Uri.Decode(cipherKey), original.Uris[0].Uri.Decode(cipherKey), StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("This cipher has a legacy URI value that the editor cannot preserve.");
        }

        return CipherRequestBuilder.EncryptLoginUris(edited.Uris, cipherKey);
    }

    private static List<EncryptedFido2Credential> BuildUpdatedCredentials(
        Fido2Credential? edited,
        Fido2Credential? projected,
        List<EncryptedFido2Credential>? original,
        ReadOnlySpan<byte> cipherKey)
    {
        int count = original?.Count ?? 0;
        if ((edited is null && count == 0) || edited?.HasSameContentAs(projected) == true)
        {
            return original ?? [];
        }

        if (count > 1)
        {
            throw new InvalidOperationException("This login has multiple passkeys; changing them is not supported.");
        }

        if (edited is null)
        {
            return [];
        }

        return [CipherRequestBuilder.EncryptFido2Credential(edited, cipherKey)];
    }

    private static void ValidateCredentialFields(CipherDataPayload originalData)
    {
        if (originalData is not LoginCipherDataPayload { Fido2Credentials: { } credentials })
        {
            return;
        }

        foreach (EncryptedFido2Credential credential in credentials)
        {
            if (credential.CredentialId.IsEmpty || credential.KeyType.IsEmpty || credential.KeyAlgorithm.IsEmpty ||
                credential.KeyCurve.IsEmpty || credential.KeyValue.IsEmpty || credential.RpId.IsEmpty ||
                credential.RpName.IsEmpty || credential.UserHandle.IsEmpty || credential.UserName.IsEmpty ||
                credential.UserDisplayName.IsEmpty || credential.Counter.IsEmpty || credential.Discoverable.IsEmpty)
            {
                throw new JsonException("Original FIDO2 credential is missing a required encrypted value.");
            }
        }
    }
}
