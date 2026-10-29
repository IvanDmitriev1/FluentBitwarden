namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultCipherAttachmentService
{
    Task DownloadAsync(
        CipherId cipherId,
        AttachmentId attachmentId,
        string destinationPath,
        CancellationToken cancellationToken);
}
