using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Services;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Shared.Services.Abstractions;
using FluentBitwarden.Views.Vault.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPageViewModel(
    IVaultSyncService vaultSyncService,
    IConnectivityService connectivityService) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] 
    public partial ObservableCollection<Cipher> FilteredCiphers { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Folder> Folders { get; private set; } = [];

    [ObservableProperty]
    public partial CipherTypeOption SelectedTypeOption { get; set; } = CipherTypeOption.ToCipherTypeOption(null);

    [ObservableProperty]
    public partial Cipher? SelectedCipher { get; set; }

    private bool _hasInitialized;

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        if (_hasInitialized || !connectivityService.HasInternetAccess)
            return;

        RefreshCollections();

        var result = await vaultSyncService.SyncVaultAsync(cancellationToken);
        if (result == VaultSyncResult.Synced)
        {
            vaultSyncService.LoadAllFromDb();
            RefreshCollections();
            return;
        }

        if (result == VaultSyncResult.Failed)
        {
            //
        }

        _hasInitialized = true;
    }

    public void OnUnloading() {}

    private void RefreshCollections()
    {
        var selectedCipherId = SelectedCipher?.Id;

        ReplaceWith(FilteredCiphers, vaultSyncService.Ciphers);
        ReplaceWith(Folders, vaultSyncService.Folders);

        SelectedCipher = selectedCipherId is null
            ? null
            : FilteredCiphers.FirstOrDefault(cipher => cipher.Id == selectedCipherId);
    }

    private static void ReplaceWith<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
