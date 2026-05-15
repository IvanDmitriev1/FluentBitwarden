using BitwardenApi.Models;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Vault.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPageViewModel(
    IMessenger messenger,
    IVaultService vaultService,
    IConnectivityService connectivityService) : ObservableRecipient(messenger), IPageLifecycleAware, IPageLifecycleRecipientAware<ShowVaultCipherMessage>
{
    [ObservableProperty]
    public partial List<VaultCipher> FilteredCiphers { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<VaultFolder> Folders { get; private set; } = [];

    [ObservableProperty] 
    public partial CipherType? SelectedCipherType { get; set; }

    [ObservableProperty]
    public partial VaultCipher? SelectedCipher { get; set; }

    [ObservableProperty] 
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CipherSortField CipherSortField { get; set; } = CipherSortField.Name;

    [ObservableProperty]
    public partial CipherSortDirection CipherSortDirection { get; set; } = CipherSortDirection.Ascending;

    [ObservableProperty]
    public partial bool IsSearchFieldOpen { get; set; }

    private bool _hasInitialized;


    partial void OnSelectedCipherTypeChanged(CipherType? value) => QueryCiphers();
    partial void OnSearchTextChanged(string value) => QueryCiphers();
    partial void OnCipherSortFieldChanged(CipherSortField value) => QueryCiphers();
    partial void OnCipherSortDirectionChanged(CipherSortDirection value) => QueryCiphers();

    public Task OnLoadingAsync(CancellationToken cancellationToken) => EnsureLoadedAsync(cancellationToken);

    public async Task OnLoadingAsync(ShowVaultCipherMessage param, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        Receive(param);
    }

    public void Receive(ShowVaultCipherMessage message)
    {
        IsSearchFieldOpen = true;
        SearchText = message.SearchText;
        SelectedCipherType = null;
        QueryCiphers();

        SelectedCipher = message.SelectedCipher;
    }


    public void OnUnloading() {}

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_hasInitialized || !connectivityService.HasInternetAccess)
            return;

        _hasInitialized = true;
        OnPropertyChanged(nameof(CipherSortField));
        OnPropertyChanged(nameof(CipherSortDirection));

        RefreshCollections();

        if (!connectivityService.HasInternetAccess)
            return;

        var result = await vaultService.SyncVaultAsync(cancellationToken);
        if (result == VaultSyncResult.Synced)
        {
            vaultService.LoadLocalVault();
            RefreshCollections();
            return;
        }

        return;

        void RefreshCollections()
        {
            var selectedCipherId = SelectedCipher?.Id;

            FilteredCiphers = vaultService.GetCiphers(CipherQuery.QueryAll);
            Folders.ReplaceWith(vaultService.GetFolders());

            SelectedCipher = selectedCipherId is null
                ? null
                : FilteredCiphers.FirstOrDefault(cipher => cipher.Id == selectedCipherId);
        }
    }

    private void QueryCiphers()
    {
        var ciphers = vaultService.GetCiphers(new CipherQuery()
        {
            SearchText = SearchText,
            CipherType = SelectedCipherType,
            SortField = CipherSortField,
            SortDirection = CipherSortDirection
        });

        FilteredCiphers = ciphers;
    }

}
