using BitwardenApi.OpenSsh;

namespace BitwardenApi.Models;

/// <summary>
/// Common vault vaultCipher fields.
/// <see cref="CardVaultCipher"/>, <see cref="IdentityVaultCipher"/>, <see cref="SecureNoteVaultCipher"/>.
/// </summary>
public abstract class VaultCipher
{
    public abstract CipherType Type { get; }

    public required CipherId Id { get; set; }
    public FolderId? FolderId { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }
    public required bool Favorite { get; set; }
    public required bool Reprompt { get; set; }
    public required DateTimeOffset RevisionDate { get; set; }
    public required DateTimeOffset CreationDate { get; set; }
    public required DateTimeOffset? DeletedDate { get; set; }
}


public sealed class LoginVaultCipher : VaultCipher
{
    public override CipherType Type => CipherType.Login;

    public string? Username { get; set; }
    public string? Password { get; set; }
    public TotpValue? Totp { get; set; }
    public List<string> Uris { get; set; } = [];
    public List<Fido2Credential> Fido2Credentials { get; set; } = [];
}

/// <remarks>Notes carries the secure note text via <see cref="VaultCipher.Notes"/>.</remarks>
public sealed class SecureNoteVaultCipher : VaultCipher
{
    public override CipherType Type => CipherType.SecureNote;
}

public sealed class CardVaultCipher : VaultCipher
{
    public override CipherType Type => CipherType.Card;

    public string? CardholderName { get; set; }
    public string? Brand { get; set; }
    public string? Number { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? Code { get; set; }
}

public sealed class IdentityVaultCipher : VaultCipher
{
    public override CipherType Type => CipherType.Identity;

    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Ssn { get; set; }
    public string? Username { get; set; }
    public string? PassportNumber { get; set; }
    public string? LicenseNumber { get; set; }
}

public sealed class SshKeyVaultCipher : VaultCipher
{
    public override CipherType Type => CipherType.SshKey;

    public OpenSshPublicKey PublicKey { get; set; } = OpenSshPublicKey.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
}
