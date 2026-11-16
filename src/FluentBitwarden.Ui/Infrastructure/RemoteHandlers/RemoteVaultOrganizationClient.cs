using FluentBitwarden.Contracts.Modules.Vault.Organizations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Ui.Infrastructure.RemoteHandlers;

internal sealed class RemoteVaultOrganizationClient(IIpcClient client) : IVaultOrganizationClient
{
    public Task<VaultOrganizationNameResponse> GetOrganizationNameAsync(
        GetVaultOrganizationNameRequest request,
        CancellationToken cancellationToken = default) =>
        client.SendAsync<GetVaultOrganizationNameRequest, VaultOrganizationNameResponse>(request, cancellationToken);
}
