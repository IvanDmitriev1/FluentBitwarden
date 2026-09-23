using FluentBitwarden.Contracts.Integrations.BrowserExtension;

namespace FluentBitwarden.BrowseProxy.Infrastructure;

internal sealed class RemoteBrowserExtensionClient(IIpcClient ipcClient) : IBrowserExtensionClient
{
    public Task<BrowserVaultStatusResponse> GetStatusAsync(BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<BrowserVaultStatusRequest, BrowserVaultStatusResponse>(request, cancellationToken);
    }

    public Task<BrowserCredentialAvailabilityResponse> CheckCredentialAvailabilityAsync(BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<BrowserCredentialAvailabilityRequest, BrowserCredentialAvailabilityResponse>(request, cancellationToken);
    }

    public Task<BrowserCredentialFillResponse> FillCredentialAsync(BrowserCredentialFillRequest request, CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<BrowserCredentialFillRequest, BrowserCredentialFillResponse>(request, cancellationToken);
    }
}

