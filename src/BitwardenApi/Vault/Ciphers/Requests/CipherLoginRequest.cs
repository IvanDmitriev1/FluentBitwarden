using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Vault.Ciphers.Models;

namespace BitwardenApi.Vault.Ciphers.Requests;
public sealed class CipherLoginRequest
{
    public EncString Username { get; init; }
    public EncString Password { get; init; }
    public EncString Totp { get; init; }
    public DateTimeOffset? PasswordRevisionDate { get; init; }
    public bool? AutofillOnPageLoad { get; init; }
    public List<EncryptedLoginUri> Uris { get; init; } = [];
    public List<EncryptedFido2Credential> Fido2Credentials { get; init; } = [];
}
