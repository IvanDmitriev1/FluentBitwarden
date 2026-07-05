using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal interface IVaultCipherAttachmentDownloadService
{
    Task DownloadAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);
}