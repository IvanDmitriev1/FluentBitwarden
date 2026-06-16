using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.BrowserExtension;
using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension;

internal sealed class BrowserExtensionClientHandlers(
    IVaultWorkspace vaultWorkspace,
    IUnlockedVaultReader unlockedVaultReader,
    IUnlockedAccountAccessor unlockedAccountAccessor) : IBrowserExtensionClient, IIpcRequestsHandler
{
    public ValueTask<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var isUnlocked = unlockedAccountAccessor.HasUnlockedAccount && vaultWorkspace.IsOpen;
        return ValueTask.FromResult(new BrowserVaultStatusResponse(IsAvailable: true, isUnlocked));
    }
}
