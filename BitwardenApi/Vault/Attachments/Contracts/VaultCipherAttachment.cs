using MemoryPack;

namespace BitwardenApi.Vault.Attachments.Contracts;

[MemoryPackable]
public sealed partial class VaultCipherAttachment
{
    [StronglyTypedIdFormatter<AttachmentId>]
    public AttachmentId Id { get; init; }

    [StronglyTypedIdFormatter<CipherId>]
    public CipherId CipherId { get; init; }

    public string FileName { get; init; } = string.Empty;
    public FileSize Size { get; init; }
}
