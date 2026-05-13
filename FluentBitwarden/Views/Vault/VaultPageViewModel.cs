using BitwardenApi.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.UI.Controls.Lifecycle;
using System.Collections.ObjectModel;
using System.Linq;
using FluentBitwarden.Infrastructure.Extensions;
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
    public partial CipherType? SelectedCipherType { get; set; }

    [ObservableProperty]
    public partial VaultCipher? SelectedCipher { get; set; }

    [ObservableProperty] 
    public partial string SearchText { get; set; } = string.Empty;


    private bool _hasInitialized;


    partial void OnSelectedCipherTypeChanged(CipherType? value) => QueryCiphers();
    partial void OnSearchTextChanged(string value) => QueryCiphers();

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

        FilteredCiphers.ReplaceWith(vaultService.GetCiphers());
        Folders.ReplaceWith(vaultService.GetFolders());

        SelectedCipher = selectedCipherId is null
            ? null
            : FilteredCiphers.FirstOrDefault(cipher => cipher.Id == selectedCipherId);
    }

    private void QueryCiphers()
    {
        var ciphers = vaultService.GetCiphers(new CipherQuery()
        {
            SearchText = SearchText,
            CipherType = SelectedCipherType,
        });

        FilteredCiphers.ReplaceWith(ciphers);
    }
}
