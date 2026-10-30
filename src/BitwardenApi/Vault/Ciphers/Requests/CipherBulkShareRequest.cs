namespace BitwardenApi.Vault.Ciphers.Requests;

internal sealed class CipherBulkShareRequest
{
    public required IReadOnlyList<CipherRequest> Ciphers { get; init; }
    public required IReadOnlyList<CollectionId> CollectionIds { get; init; }
}
