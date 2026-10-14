using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

namespace FluentBitwarden.Contracts.Integrations.BrowserExtension;

public interface IBrowserExtensionClient
{
    Task<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<BrowserCredentialAvailabilityResponse> CheckCredentialAvailabilityAsync(
        BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<BrowserCredentialFillResponse> FillCredentialAsync(
        BrowserCredentialFillRequest request,
        CancellationToken cancellationToken = default);
}
