using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

namespace FluentBitwarden.Contracts.Modules.BrowserExtension;

public interface IBrowserExtensionClient
{
    ValueTask<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default);
}