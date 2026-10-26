using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Ciphers.Requests;
public sealed class CipherLoginRequest
{
    public EncString Username { get; init; }
    public EncString Password { get; init; }
    public EncString Totp { get; init; }
    public List<CipherLoginUriRequest> Uris { get; init; } = [];
    public List<CipherFido2CredentialRequest> Fido2Credentials { get; init; } = [];
}
