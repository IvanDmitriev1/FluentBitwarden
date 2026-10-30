namespace BitwardenApi.Vault.Ciphers.Requests;

internal sealed class CipherBulkDeleteRequest
{
    public required IReadOnlyList<CipherId> Ids { get; init; }
}
