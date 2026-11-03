using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Vault.Ciphers.Models;

namespace BitwardenApi.Vault.Ciphers.Serialization;

internal abstract class CipherDataPayload
{
    public EncString Name { get; init; }
    public EncString Notes { get; init; }
    public List<EncryptedCipherField>? Fields { get; init; }
    public List<EncryptedPasswordHistory>? PasswordHistory { get; init; }
}

internal sealed class LoginCipherDataPayload : CipherDataPayload
{
    public EncString Uri { get; init; }
    public List<EncryptedLoginUri>? Uris { get; init; }
    public EncString Username { get; init; }
    public EncString Password { get; init; }
    public DateTimeOffset? PasswordRevisionDate { get; init; }
    public EncString Totp { get; init; }
    public bool? AutofillOnPageLoad { get; init; }
    public List<EncryptedFido2Credential>? Fido2Credentials { get; init; }
}

internal sealed class SecureNoteCipherDataPayload : CipherDataPayload
{
    public int Type { get; init; }
}

internal sealed class CardCipherDataPayload : CipherDataPayload
{
    public EncString CardholderName { get; init; }
    public EncString Brand { get; init; }
    public EncString Number { get; init; }
    public EncString ExpMonth { get; init; }
    public EncString ExpYear { get; init; }
    public EncString Code { get; init; }
}

internal sealed class IdentityCipherDataPayload : CipherDataPayload
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
    public EncString SSN { get; init; }
    public EncString Username { get; init; }
    public EncString PassportNumber { get; init; }
    public EncString LicenseNumber { get; init; }
}

internal sealed class SshKeyCipherDataPayload : CipherDataPayload
{
    public EncString PrivateKey { get; init; }
    public EncString PublicKey { get; init; }
    public EncString KeyFingerprint { get; init; }
}
