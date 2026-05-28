using BitwardenApi.Common.MemoryPackFormatters;
using MemoryPack;

namespace BitwardenApi.Models;

[MemoryPackable]
public sealed partial class VaultFolder
{
    [StronglyTypedIdFormatter<FolderId>]
    public required FolderId Id { get; init; }

    public required string Name { get; init; }
    public DateTimeOffset RevisionDate { get; init; }
}
