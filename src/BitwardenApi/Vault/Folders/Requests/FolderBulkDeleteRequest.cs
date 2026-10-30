namespace BitwardenApi.Vault.Folders.Requests;

internal sealed class FolderBulkDeleteRequest
{
    public required IReadOnlyList<FolderId> Ids { get; init; }
}
