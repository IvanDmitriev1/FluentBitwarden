namespace BitwardenApi.Vault.Ciphers.Requests;

internal sealed class CipherBulkMoveRequest
{
    public required IReadOnlyList<CipherId> Ids { get; init; }
    public string? FolderId { get; init; }
}
