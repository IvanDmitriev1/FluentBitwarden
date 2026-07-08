using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal interface IVaultCipherAttachmentDownloadService
{
    Task DownloadAsync(
        BitwardenAccountContext accountContext,
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);
}