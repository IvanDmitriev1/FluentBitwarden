using FluentBitwarden.AppHost.Modules.BrowserExtension;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.BrowserExtension;
using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

internal sealed class BrowserExtensionIpcHandler(BrowserExtensionService browserExtensionService)
    : IBrowserExtensionClient, IIpcRequestsHandler
{
    public ValueTask<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default) =>
        browserExtensionService.GetStatusAsync(request, cancellationToken);

    public ValueTask<BrowserCredentialAvailabilityResponse> CheckCredentialAvailabilityAsync(
        BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default) =>
        browserExtensionService.CheckCredentialAvailabilityAsync(request, cancellationToken);

    public ValueTask<BrowserCredentialFillResponse> FillCredentialAsync(
        BrowserCredentialFillRequest request,
        CancellationToken cancellationToken = default) =>
        browserExtensionService.FillCredentialAsync(request, cancellationToken);
}
