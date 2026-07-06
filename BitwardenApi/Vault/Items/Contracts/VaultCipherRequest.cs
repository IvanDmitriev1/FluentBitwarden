namespace BitwardenApi.Vault.Items.Contracts;

/// <summary>
/// Structured request body for <c>POST /ciphers</c> and <c>PUT /ciphers/{id}</c>.
/// Mirrors Bitwarden server's <c>CipherRequestModel</c> DTO; exposed here as
/// <see cref="VaultCipherRequest"/>. The server derives the stored
/// <c>data</c> blob from these fields. Encrypted fields are <see cref="EncString"/> and
/// serialize to the wire form <c>"2.iv|data|mac"</c>.
/// </summary>
public sealed class VaultCipherRequest
{
    public required VaultCipherType Type { get; init; }

    /// <summary>
    /// The user the payload is encrypted for; the server validates it matches the caller.
    /// For personal ciphers this is the current user id.
    /// </summary>
    public Guid? EncryptedFor { get; init; }

    /// <summary>Owning folder id (personal ciphers only); null when unfiled.</summary>
    public string? FolderId { get; init; }

    public required bool Favorite { get; init; }

    /// <summary>Password re-prompt: 0 = none, 1 = password.</summary>
    public required int Reprompt { get; init; }

    /// <summary>The individual cipher key, wrapped by the vault key.</summary>
    public required EncString Key { get; init; }

    public required EncString Name { get; init; }
    public EncString Notes { get; init; }

    public CipherLoginRequest? Login { get; init; }
    public CipherCardRequest? Card { get; init; }
    public CipherIdentityRequest? Identity { get; init; }
    public CipherSecureNoteRequest? SecureNote { get; init; }
    public CipherSshKeyRequest? SshKey { get; init; }

    /// <summary>Optimistic-concurrency guard sent on update; the last revision date we hold.</summary>
    public DateTime? LastKnownRevisionDate { get; init; }
}

public sealed class CipherLoginRequest
{
    public EncString Username { get; init; }
    public EncString Password { get; init; }
    public EncString Totp { get; init; }
    public List<CipherLoginUriRequest> Uris { get; init; } = [];
    public List<CipherFido2CredentialRequest> Fido2Credentials { get; init; } = [];
}

public sealed class CipherLoginUriRequest
{
    public required EncString Uri { get; init; }

    /// <summary>URI match type: 0 = domain, 1 = host, ... null = default (domain).</summary>
    public int? Match { get; init; }
}

/// <summary>
/// A FIDO2 (WebAuthn) credential stored on a login cipher. Every field except
/// <see cref="CreationDate"/> is encrypted; byte-array values are base64url text before encryption.
/// </summary>
public sealed class CipherFido2CredentialRequest
{
    public required EncString CredentialId { get; init; }
    public required EncString KeyType { get; init; }
    public required EncString KeyAlgorithm { get; init; }
    public required EncString KeyCurve { get; init; }
    public required EncString KeyValue { get; init; }
    public required EncString RpId { get; init; }
    public required EncString RpName { get; init; }
    public required EncString UserHandle { get; init; }
    public required EncString UserName { get; init; }
    public required EncString UserDisplayName { get; init; }
    public required EncString Counter { get; init; }
    public required EncString Discoverable { get; init; }
    public required DateTime CreationDate { get; init; }
}

public sealed class CipherCardRequest
{
    public EncString CardholderName { get; init; }
    public EncString Brand { get; init; }
    public EncString Number { get; init; }
    public EncString ExpMonth { get; init; }
    public EncString ExpYear { get; init; }
    public EncString Code { get; init; }
}

public sealed class CipherIdentityRequest
{
    public EncString Title { get; init; }
    public EncString FirstName { get; init; }
    public EncString MiddleName { get; init; }
    public EncString LastName { get; init; }
    public EncString Address1 { get; init; }
    public EncString Address2 { get; init; }
    public EncString Address3 { get; init; }
    public EncString City { get; init; }
    public EncString State { get; init; }
    public EncString PostalCode { get; init; }
    public EncString Country { get; init; }
    public EncString Company { get; init; }
    public EncString Email { get; init; }
    public EncString Phone { get; init; }
    public EncString Ssn { get; init; }
    public EncString Username { get; init; }
    public EncString PassportNumber { get; init; }
    public EncString LicenseNumber { get; init; }
}

public sealed class CipherSecureNoteRequest
{
    /// <summary>Secure note type: 0 = generic.</summary>
    public required int Type { get; init; }
}

public sealed class CipherSshKeyRequest
{
    public required EncString PrivateKey { get; init; }
    public required EncString PublicKey { get; init; }
    public required EncString KeyFingerprint { get; init; }
}
