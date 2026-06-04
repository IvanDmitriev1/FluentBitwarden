using BitwardenApi.Models;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Platform;
using FluentBitwarden.Platform.SiteIcons;
using FluentBitwarden.Views.Vault.Browse.Models;
using System.Collections.ObjectModel;
using Windows.Networking.Connectivity;

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
    private bool _applyingParameter;

    partial void OnSelectedCipherTypeChanged(CipherType? value) => _ = QueryCiphersAsync();
    partial void OnSearchTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            IsSearchFieldOpen = true;

        if (!_applyingParameter)
            _ = QueryCiphersAsync();
    }


    partial void OnSelectedCipherChanged(VaultCipher? value)
    {
        if (value is null)
        {
            SettingsStore.Instance.SetComposite(UiSettingKeys.Vault.StateKey, VaultBrowseState.Default);
            return;
        }

        var composite = new VaultBrowseState(SearchText, value.Id);
        SettingsStore.Instance.SetComposite(UiSettingKeys.Vault.StateKey, composite);
    }

    partial void OnCipherSortFieldChanged(VaultCipherSortField value) => _ = QueryCiphersAsync();
    partial void OnCipherSortDirectionChanged(VaultCipherSortDirection value) => _ = QueryCiphersAsync();

    public Task OnLoadingAsync(CancellationToken cancellationToken) => EnsureLoadedAsync(cancellationToken);

    public async Task OnLoadingAsync(ShowVaultCipherMessage param, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        Receive(param);
    }

    public async void Receive(ShowVaultCipherMessage message)
    {
        _applyingParameter = true;
        SearchText = message.SearchText;
        SelectedCipherType = null;

        await QueryCiphersAsync();
        SelectedCipher = FilteredCiphers.FirstOrDefault(c => c.Id == message.SelectedCipher.Id);
        _applyingParameter = false;
    }


    public void OnUnloading() {}

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_hasInitialized || !NetworkInformation.HasInternetAccess)
            return;

        _hasInitialized = true;
        _applyingParameter = true;
        OnPropertyChanged(nameof(CipherSortField));
        OnPropertyChanged(nameof(CipherSortDirection));

        Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));

        var state = SettingsStore.Instance.GetComposite(UiSettingKeys.Vault.StateKey);
        SearchText = state.SearchText;

        await QueryCiphersAsync();
        SelectedCipher = FilteredCiphers.FirstOrDefault(c => c.Id == state.SelectedCipherId);
        _applyingParameter = false;

        var result = await vaultClient.SyncVaultAsync(cancellationToken);
        if (result != VaultSyncResult.Synced)
            return;

        Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));

        var selectedCipherId = SelectedCipher?.Id;
        await QueryCiphersAsync();

        SelectedCipher = selectedCipherId is null
            ? null
            : FilteredCiphers.FirstOrDefault(cipher => cipher.Id == selectedCipherId);

        _ = PreloadSiteIconsAsync();
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
