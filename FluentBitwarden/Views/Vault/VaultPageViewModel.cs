using BitwardenApi.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Resources.Controls.Lifecycle;
using FluentBitwarden.Views.Vault.Models;
using System.Collections.ObjectModel;
using System.Linq;
using FluentBitwarden.Infrastructure.Services.Abstractions;

namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPageViewModel(
    IVaultService vaultService,
    IConnectivityService connectivityService) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] 
    public partial ObservableCollection<VaultCipher> FilteredCiphers { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<VaultFolder> Folders { get; private set; } = [];

    [ObservableProperty]
    public partial CipherTypeOption SelectedTypeOption { get; set; } = CipherTypeOption.ToCipherTypeOption(null);

    [ObservableProperty]
    public partial VaultCipher? SelectedCipher { get; set; }

    private bool _hasInitialized;

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        if (_hasInitialized || !connectivityService.HasInternetAccess)
            return;

        RefreshCollections();

        var result = await vaultService.SyncVaultAsync(cancellationToken);
        if (result == VaultSyncResult.Synced)
        {
            vaultService.LoadLocalVault();
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

        ReplaceWith(FilteredCiphers, vaultService.GetCiphers());
        ReplaceWith(Folders, vaultService.GetFolders());

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
