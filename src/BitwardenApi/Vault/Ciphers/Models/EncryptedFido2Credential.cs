using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Vault.Ciphers.Models;

public sealed class EncryptedFido2Credential
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
    public required DateTimeOffset CreationDate { get; init; }
}
