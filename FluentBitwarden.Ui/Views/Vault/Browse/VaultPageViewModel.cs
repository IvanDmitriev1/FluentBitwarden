using BitwardenApi.Models;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using System.Collections.ObjectModel;
using Windows.Networking.Connectivity;
using FluentBitwarden.Views.Vault.Browse.Models;
using FluentBitwarden.Views.Vault.Browse.SiteIcons;

namespace FluentBitwarden.Views.Vault.Browse;

public sealed partial class VaultPageViewModel(
    IMessenger messenger,
    IVaultClient vaultClient,
    ISiteIconCache siteIconCache) : ObservableRecipient(messenger), IPageLifecycleAware, IPageLifecycleRecipientAware<ShowVaultCipherMessage>
{
    [ObservableProperty]
    public partial VaultCipher[] FilteredCiphers { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<VaultFolder> Folders { get; private set; } = [];

    [ObservableProperty] 
    public partial CipherType? SelectedCipherType { get; set; }

    [ObservableProperty]
    public partial VaultCipher? SelectedCipher { get; set; }

    [ObservableProperty] 
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial VaultCipherSortField CipherSortField { get; set; } = VaultCipherSortField.Name;

    [ObservableProperty]
    public partial VaultCipherSortDirection CipherSortDirection { get; set; } = VaultCipherSortDirection.Ascending;

    [ObservableProperty]
    public partial bool IsSearchFieldOpen { get; set; }

    private bool _hasInitialized;


    partial void OnSelectedCipherTypeChanged(CipherType? value) => _ = QueryCiphersAsync();
    partial void OnSearchTextChanged(string value) => _ = QueryCiphersAsync();
    partial void OnCipherSortFieldChanged(VaultCipherSortField value) => _ = QueryCiphersAsync();
    partial void OnCipherSortDirectionChanged(VaultCipherSortDirection value) => _ = QueryCiphersAsync();

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
        _ = QueryCiphersAsync();

        SelectedCipher = message.SelectedCipher;
    }


    public void OnUnloading() {}

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_hasInitialized || !NetworkInformation.HasInternetAccess)
            return;

        _hasInitialized = true;
        OnPropertyChanged(nameof(CipherSortField));
        OnPropertyChanged(nameof(CipherSortDirection));

        await RefreshCollections();

        if (!NetworkInformation.HasInternetAccess)
            return;

        var result = await vaultClient.SyncVaultAsync(cancellationToken);
        if (result == VaultSyncResult.Synced)
        {
            await RefreshCollections();
        }

        return;
        async Task RefreshCollections()
        {
            var selectedCipherId = SelectedCipher?.Id;

            await QueryCiphersAsync();
            Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));

            SelectedCipher = selectedCipherId is null
                ? null
                : FilteredCiphers.FirstOrDefault(cipher => cipher.Id == selectedCipherId);

            _ = PreloadSiteIconsAsync();
        }
    }

    private async Task QueryCiphersAsync()
    {
        var ciphers = await vaultClient.SearchCiphersAsync(new VaultCipherQuery()
        {
            SearchText = SearchText,
            CipherType = SelectedCipherType,
            SortField = CipherSortField,
            SortDirection = CipherSortDirection
        });

        FilteredCiphers = ciphers;
    }

    private Task PreloadSiteIconsAsync()
    {
        var urls = FilteredCiphers
            .OfType<LoginVaultCipher>()
            .Select(static c => c.Uris.FirstOrDefault())
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null)
            .Where(static uri => uri is not null)
            .Cast<Uri>()
            .ToList();

        return siteIconCache.PreloadAsync(urls);
    }
}
