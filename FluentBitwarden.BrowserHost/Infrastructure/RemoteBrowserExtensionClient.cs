using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.BrowserExtension;

namespace FluentBitwarden.BrowserHost.Infrastructure;

internal sealed class RemoteBrowserExtensionClient(IIpcClient ipcClient) : IBrowserExtensionClient
{
    public ValueTask<BrowserVaultStatusResponse> GetStatusAsync(BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<BrowserVaultStatusRequest, BrowserVaultStatusResponse>(request, cancellationToken);
    }

    public ValueTask<BrowserCredentialAvailabilityResponse> CheckCredentialAvailabilityAsync(BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<BrowserCredentialAvailabilityRequest, BrowserCredentialAvailabilityResponse>(request, cancellationToken);
    }

    public ValueTask<BrowserCredentialFillResponse> FillCredentialAsync(BrowserCredentialFillRequest request, CancellationToken cancellationToken = default)
    {
        return ipcClient.SendAsync<BrowserCredentialFillRequest, BrowserCredentialFillResponse>(request, cancellationToken);
    }
}

