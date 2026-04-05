namespace BitwardenApi.Modules.Vault.Models;

/// <summary>
/// Common vault cipher fields.
/// <see cref="CardCipher"/>, <see cref="IdentityCipher"/>, <see cref="SecureNoteCipher"/>.
/// </summary>
public abstract class Cipher
{
    public required CipherId Id { get; set; }
    public FolderId? FolderId { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }
    public bool Favorite { get; set; }
    public bool Reprompt { get; set; }
    public DateTimeOffset RevisionDate { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? DeletedDate { get; set; }
}


public sealed class LoginCipher : Cipher
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Totp { get; set; }
    public required List<string> Uris { get; set; }
}

/// <remarks>Notes carries the secure note text via <see cref="Cipher.Notes"/>.</remarks>
public sealed class SecureNoteCipher : Cipher { }

public sealed class CardCipher : Cipher
{
    public string? CardholderName { get; set; }
    public string? Brand { get; set; }
    public string? Number { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? Code { get; set; }
}

public sealed class IdentityCipher : Cipher
{
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