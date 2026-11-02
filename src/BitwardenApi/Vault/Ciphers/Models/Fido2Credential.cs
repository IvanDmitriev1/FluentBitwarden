using MemoryPack;

namespace BitwardenApi.Vault.Ciphers.Models;

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

    internal bool HasSameContentAs(Fido2Credential? other)
        => other is not null &&
           CredentialId.AsSpan().SequenceEqual(other.CredentialId) &&
           KeyType == other.KeyType &&
           KeyAlgorithm == other.KeyAlgorithm &&
           KeyCurve == other.KeyCurve &&
           KeyValue.AsSpan().SequenceEqual(other.KeyValue) &&
           string.Equals(RpId, other.RpId, StringComparison.Ordinal) &&
           string.Equals(RpName, other.RpName, StringComparison.Ordinal) &&
           UserHandle.AsSpan().SequenceEqual(other.UserHandle) &&
           string.Equals(UserName, other.UserName, StringComparison.Ordinal) &&
           string.Equals(UserDisplayName, other.UserDisplayName, StringComparison.Ordinal) &&
           Counter == other.Counter &&
           Discoverable == other.Discoverable &&
           CreationDate == other.CreationDate;
}
