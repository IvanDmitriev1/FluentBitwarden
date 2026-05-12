namespace BitwardenApi.Models;

public sealed class VaultFolder
{
    public required FolderId Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset RevisionDate { get; init; }
}
