using MemoryPack;

namespace BitwardenApi.Vault.Items.Contracts;

public enum Fido2CredentialKeyType
{
    PublicKey = 1
}

public enum Fido2CredentialKeyAlgorithm
{
    Ecdsa = 1
}

public enum Fido2CredentialKeyCurve
{
    P256 = 1
}


[MemoryPackable]
public sealed partial class Fido2Credential
{
    public required byte[] CredentialId { get; init; }
    public required Fido2CredentialKeyType KeyType { get; init; }
    public required Fido2CredentialKeyAlgorithm KeyAlgorithm { get; init; }
    public required Fido2CredentialKeyCurve KeyCurve { get; init; }
    public required byte[] KeyValue { get; init; }
    public required string RpId { get; init; }
    public required string RpName { get; init; }
    public required byte[] UserHandle { get; init; }
    public required string UserName { get; init; }
    public required string UserDisplayName { get; init; }
    public required uint Counter { get; init; }
    public required bool Discoverable { get; init; }
    public required DateTimeOffset CreationDate { get; init; }
}
