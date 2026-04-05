namespace BitwardenApi.Modules.Vault.Models;

public sealed class Folder
{
    public required FolderId Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset RevisionDate { get; init; }
}
