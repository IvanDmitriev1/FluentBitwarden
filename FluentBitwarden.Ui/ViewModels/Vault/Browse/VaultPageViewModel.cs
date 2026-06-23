using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using System.Collections.ObjectModel;
using Windows.Networking.Connectivity;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.ViewModels.Vault.Browse;

public sealed partial class VaultPageViewModel(
    IVaultClient vaultClient,
    ISiteIconCache siteIconCache) : ObservableObject, IPageLifecycleAware, IPageLifecycleAware<ShowVaultCipherMessage>
{
    [ObservableProperty]
    public partial VaultCipher[] FilteredCiphers { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<VaultFolder> Folders { get; private set; } = [];

    [ObservableProperty]
    public partial VaultCipherType? SelectedCipherType { get; set; }

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

    partial void OnSelectedCipherTypeChanged(VaultCipherType? value) => _ = QueryCiphersAsync();
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
        await ApplyNavigationAsync(param, cancellationToken);
    }

    private async Task ApplyNavigationAsync(
        ShowVaultCipherMessage message,
        CancellationToken cancellationToken)
    {
        _applyingParameter = true;
        try
        {
            SearchText = message.SearchText;
            SelectedCipherType = null;

            await QueryCiphersAsync(cancellationToken);
            SelectedCipher = FilteredCiphers.FirstOrDefault(c => c.Id == message.SelectedCipher.Id);
        }
        finally
        {
            _applyingParameter = false;
        }
    }


    public void OnUnloading() { }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_hasInitialized)
            return;

        _applyingParameter = true;
        try
        {
            OnPropertyChanged(nameof(CipherSortField));
            OnPropertyChanged(nameof(CipherSortDirection));

            Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));

            var state = SettingsStore.Instance.GetComposite(UiSettingKeys.Vault.StateKey);
            SearchText = state.SearchText;

            await QueryCiphersAsync(cancellationToken);
            SelectedCipher = FilteredCiphers.FirstOrDefault(c => c.Id == state.SelectedCipherId);

            await SyncVault(cancellationToken);
            _hasInitialized = true;
        }
        finally
        {
            _applyingParameter = false;
        }
    }

    private async Task QueryCiphersAsync(CancellationToken cancellationToken = default)
    {
        var ciphers = await vaultClient.SearchCiphersAsync(new VaultCipherQuery()
        {
            SearchText = SearchText,
            CipherType = SelectedCipherType,
            SortField = CipherSortField,
            SortDirection = CipherSortDirection
        }, cancellationToken);

        FilteredCiphers = ciphers;
    }

    private async Task SyncVault(CancellationToken cancellationToken)
    {
        var result = await vaultClient.SyncVaultAsync(cancellationToken);
        if (result != VaultSyncResult.Synced)
            return;

        Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));

        var selectedCipherId = SelectedCipher?.Id;
        await QueryCiphersAsync(cancellationToken);

        SelectedCipher = selectedCipherId is null
            ? null
            : FilteredCiphers.FirstOrDefault(cipher => cipher.Id == selectedCipherId);

        if (NetworkInformation.HasInternetAccess)
        {
            _ = PreloadSiteIconsAsync();
        }
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
