using MemoryPack;




using BitwardenApi.Serialization;

namespace BitwardenApi.Vault.Folders.Models;

[MemoryPackable]
public sealed partial class Folder
{
    [StronglyTypedIdFormatter<FolderId>]
    public FolderId Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RevisionDate { get; set; }
}
