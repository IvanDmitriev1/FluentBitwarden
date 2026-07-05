using BitwardenApi.Vault.Attachments.Contracts;
using MemoryPack;

namespace BitwardenApi.Vault.Items.Contracts;

/// <summary>
/// Common vault vaultCipher fields.
/// <see cref="CardVaultCipher"/>, <see cref="IdentityVaultCipher"/>, <see cref="SecureNoteVaultCipher"/>.
/// </summary>
[MemoryPackable]
[MemoryPackUnion(0, typeof(LoginVaultCipher))]
[MemoryPackUnion(1, typeof(SecureNoteVaultCipher))]
[MemoryPackUnion(2, typeof(CardVaultCipher))]
[MemoryPackUnion(3, typeof(IdentityVaultCipher))]
[MemoryPackUnion(4, typeof(SshKeyVaultCipher))]
public abstract partial class VaultCipher
{
    [MemoryPackIgnore]
    public abstract VaultCipherType Type { get; }

    [StronglyTypedIdFormatter<CipherId>]
    public required CipherId Id { get; set; }

    [StronglyTypedIdFormatter<FolderId>]
    public FolderId FolderId { get; set; }

    public required string Name { get; set; }
    public string? Notes { get; set; }
    public required bool Favorite { get; set; }
    public required bool Reprompt { get; set; }
    public required DateTimeOffset RevisionDate { get; set; }
    public required DateTimeOffset CreationDate { get; set; }
    public required DateTimeOffset? DeletedDate { get; set; }

    public VaultCipherAttachment[] Attachments { get; set; } = [];
}

[MemoryPackable]
public sealed partial class LoginVaultCipher : VaultCipher
{
    public override VaultCipherType Type => VaultCipherType.Login;

    public string? Username { get; set; }
    public string? Password { get; set; }

    [TotpValueFormatter]
    public TotpValue? Totp { get; set; }
    public List<LoginUri> Uris { get; set; } = [];
    public Fido2Credential? Fido2Credential { get; set; }
}

/// <remarks>Notes carries the secure note text via <see cref="VaultCipher.Notes"/>.</remarks>
[MemoryPackable]
public sealed partial class SecureNoteVaultCipher : VaultCipher
{
    public override VaultCipherType Type => VaultCipherType.SecureNote;
}

[MemoryPackable]
public sealed partial class CardVaultCipher : VaultCipher
{
    public override VaultCipherType Type => VaultCipherType.Card;

    public string? CardholderName { get; set; }
    public string? Brand { get; set; }
    public string? Number { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? Code { get; set; }
}

[MemoryPackable]
public sealed partial class IdentityVaultCipher : VaultCipher
{
    public override VaultCipherType Type => VaultCipherType.Identity;

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

[MemoryPackable]
public sealed partial class SshKeyVaultCipher : VaultCipher
{
    public override VaultCipherType Type => VaultCipherType.SshKey;

    public OpenSshPublicKey PublicKey { get; set; } = OpenSshPublicKey.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
}
