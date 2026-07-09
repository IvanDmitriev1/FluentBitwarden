using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal interface IVaultCipherAttachmentDownloadService
{
    Task DownloadAsync(
        BitwardenAccountContext accountContext,
        KeySession keys,
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
