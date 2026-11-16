using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Organizations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultOrganizationIpcHandler(
    IAppSessionAccessor sessionAccessor,
    IVaultOrganizationReader vaultReader) : IVaultOrganizationClient, IIpcRequestsHandler
{
    public Task<VaultOrganizationNameResponse> GetOrganizationNameAsync(
        GetVaultOrganizationNameRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var account = sessionAccessor.RequireUnlockedState().Account;
        var name = request.OrganizationId.IsEmpty
            ? null
            : vaultReader.GetOrganizationName(account.UserId, request.OrganizationId);
        return Task.FromResult(new VaultOrganizationNameResponse(name));
    }
}
