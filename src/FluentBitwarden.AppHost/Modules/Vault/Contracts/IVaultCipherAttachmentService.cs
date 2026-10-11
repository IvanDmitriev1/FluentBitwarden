using FluentBitwarden.AppHost.AppSession.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultCipherAttachmentService
{
    Task DownloadAsync(
        IUnlockedSessionLease session,
        CipherId cipherId,
        AttachmentId attachmentId,
        string destinationPath,
        CancellationToken cancellationToken);
}
