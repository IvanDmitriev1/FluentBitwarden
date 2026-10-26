using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Ciphers.Requests;
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
