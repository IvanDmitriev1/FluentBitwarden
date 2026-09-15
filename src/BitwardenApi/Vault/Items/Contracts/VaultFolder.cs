using MemoryPack;

namespace BitwardenApi.Vault.Items.Contracts;

[MemoryPackable]
public sealed partial class VaultFolder
{
    [StronglyTypedIdFormatter<FolderId>]
    public FolderId Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RevisionDate { get; set; }
}
