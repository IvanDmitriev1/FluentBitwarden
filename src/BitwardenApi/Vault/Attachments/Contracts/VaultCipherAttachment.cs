using MemoryPack;

namespace BitwardenApi.Vault.Attachments.Contracts;

[MemoryPackable]
public sealed partial class VaultCipherAttachment
{
    [StronglyTypedIdFormatter<AttachmentId>]
    public AttachmentId Id { get; set; }

    [StronglyTypedIdFormatter<CipherId>]
    public CipherId CipherId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public FileSize Size { get; set; }
}
