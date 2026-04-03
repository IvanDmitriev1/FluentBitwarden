using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPageViewModel(IVaultSyncService vaultSyncService) : ObservableObject, IPageLifecycleAware
{
    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        
    }

    public void OnUnloading() {}
}