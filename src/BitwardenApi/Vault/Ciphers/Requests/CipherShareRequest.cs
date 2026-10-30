namespace BitwardenApi.Vault.Ciphers.Requests;

internal sealed class CipherShareRequest
{
    public required CipherRequest Cipher { get; init; }
    public required IReadOnlyList<CollectionId> CollectionIds { get; init; }
}
