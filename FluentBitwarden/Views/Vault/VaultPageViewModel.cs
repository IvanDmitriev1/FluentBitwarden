using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Views.Vault.Models;
using System.Collections.ObjectModel;

namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPageViewModel(IVaultSyncService vaultSyncService) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] 
    public partial ObservableCollection<Cipher> FilteredCiphers { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Folder> Folders { get; private set; } = [];

    [ObservableProperty]
    public partial CipherTypeOption SelectedTypeOption { get; set; } = CipherTypeOption.ToCipherTypeOption(null);

    [ObservableProperty]
    public partial Cipher? SelectedCipher { get; set; }

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        RefreshCollections();

        var result = await vaultSyncService.SyncVaultAsync();
        if (result == VaultSyncResult.Synced)
        {
            RefreshCollections();
            return;
        }

        if (result == VaultSyncResult.Failed)
        {
            //
        }
    }

    public void OnUnloading() {}

    private void RefreshCollections()
    {
        FilteredCiphers = new ObservableCollection<Cipher>(vaultSyncService.Ciphers);
        Folders = new ObservableCollection<Folder>(Folders);
    }
}