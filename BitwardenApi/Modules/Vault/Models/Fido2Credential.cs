namespace BitwardenApi.Modules.Vault.Models;

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


public sealed class Fido2Credential
{
    public required string CredentialId { get; init; }
    public required Fido2CredentialKeyType KeyType { get; init; }
    public required Fido2CredentialKeyAlgorithm KeyAlgorithm { get; init; }
    public required Fido2CredentialKeyCurve KeyCurve { get; init; }
    public required string KeyValue { get; init; }
    public required string RpId { get; init; }
    public required string RpName { get; init; }
    public required string UserHandle { get; init; }
    public required string UserName { get; init; }
    public required string UserDisplayName { get; init; }
    public required int Counter { get; init; }
    public required bool Discoverable { get; init; }
    public required DateTimeOffset CreationDate { get; init; }
}