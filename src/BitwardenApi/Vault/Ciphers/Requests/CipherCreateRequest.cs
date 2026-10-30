namespace BitwardenApi.Vault.Ciphers.Requests;

internal sealed class CipherCreateRequest
{
    public required CipherRequest Cipher { get; init; }
    public required IReadOnlyList<CollectionId> CollectionIds { get; init; }
}
