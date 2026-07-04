namespace BitwardenApi.Vault.Attachments.Contracts;

public delegate Task VaultCipherAttachmentStreamHandler(
    Stream encryptedAttachmentStream,
    EncString encryptedAttachmentKey);