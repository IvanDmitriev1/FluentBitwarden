using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Ciphers.Requests;
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
