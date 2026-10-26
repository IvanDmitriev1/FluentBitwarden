using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Attachments;

public delegate Task AttachmentStreamHandler(
    Stream encryptedAttachmentStream,
    EncString encryptedAttachmentKey);