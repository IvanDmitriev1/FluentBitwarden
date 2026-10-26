using MemoryPack;

using BitwardenApi.Primitives;
using BitwardenApi.Serialization;

namespace BitwardenApi.Vault.Attachments.Models;

[MemoryPackable]
public sealed partial class CipherAttachment
{
    [StronglyTypedIdFormatter<AttachmentId>]
    public AttachmentId Id { get; set; }

    [StronglyTypedIdFormatter<CipherId>]
    public CipherId CipherId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public FileSize Size { get; set; }
}
