namespace BitwardenApi.Vault.Ciphers.Requests;

internal sealed class CipherPartialRequest
{
    public string? FolderId { get; init; }
    public required bool Favorite { get; init; }
}
