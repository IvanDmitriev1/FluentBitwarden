using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

namespace FluentBitwarden.Contracts.Modules.BrowserExtension;

public interface IBrowserExtensionClient
{
    ValueTask<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<BrowserCredentialAvailabilityResponse> CheckCredentialAvailabilityAsync(
        BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<BrowserCredentialFillResponse> FillCredentialAsync(
        BrowserCredentialFillRequest request,
        CancellationToken cancellationToken = default);
}