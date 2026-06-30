using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using System.Collections.ObjectModel;
using Windows.Networking.Connectivity;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.ViewModels.Vault.Browse;

public sealed partial class VaultPageViewModel(
    IVaultClient vaultClient,
    ISiteIconCache siteIconCache) : ObservableObject, IPageLifecycleAware, IPageLifecycleAware<ShowVaultCipherIntent>, IPageLifecycleAware<OpenVaultCipherIntent>
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

    partial void OnSelectedCipherTypeChanged(VaultCipherType? value)
    {
        if (!_applyingParameter)
            _ = QueryCiphersAsync();
    }

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

    partial void OnCipherSortFieldChanged(VaultCipherSortField value)
    {
        if (!_applyingParameter)
            _ = QueryCiphersAsync();
    }

    partial void OnCipherSortDirectionChanged(VaultCipherSortDirection value)
    {
        if (!_applyingParameter)
            _ = QueryCiphersAsync();
    }

    public Task OnLoadingAsync(CancellationToken cancellationToken) =>
        EnsureLoadedAsync(cancellationToken);

    public Task OnLoadingAsync(ShowVaultCipherIntent param, CancellationToken cancellationToken) =>
        LoadOrApplyNavigationAsync(param, cancellationToken);

    public async Task OnLoadingAsync(OpenVaultCipherIntent param, CancellationToken cancellationToken)
    {
        var cipher = await vaultClient.GetCipherAsync(new GetVaultCipherRequest(param.CipherId), cancellationToken);

        if (cipher is null)
        {
            await EnsureLoadedAsync(cancellationToken);
            return;
        }

        await LoadOrApplyNavigationAsync(new ShowVaultCipherIntent(string.Empty, cipher), cancellationToken);
    }

    private Task LoadOrApplyNavigationAsync(
        ShowVaultCipherIntent intent,
        CancellationToken cancellationToken) => _hasInitialized
        ? ApplyNavigationAsync(intent, cancellationToken)
        : EnsureLoadedAsync(cancellationToken, intent);

    private async Task ApplyNavigationAsync(
        ShowVaultCipherIntent message,
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

    private async Task EnsureLoadedAsync(
        CancellationToken cancellationToken,
        ShowVaultCipherIntent? initialNavigation = null)
    {
        if (_hasInitialized)
            return;

        _applyingParameter = true;
        try
        {
            OnPropertyChanged(nameof(CipherSortField));
            OnPropertyChanged(nameof(CipherSortDirection));

            Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));
            await QueryCiphersAsync(cancellationToken);

            var state = SettingsStore.Instance.GetComposite(UiSettingKeys.Vault.StateKey);
            CipherId selectedCipherId;
            if (initialNavigation is null)
            {
                SearchText = state.SearchText;
                selectedCipherId = state.SelectedCipherId;
            }
            else
            {
                SearchText = initialNavigation.SearchText;
                SelectedCipherType = null;
                selectedCipherId = initialNavigation.SelectedCipher.Id;
            }

            SelectedCipher = selectedCipherId == CipherId.Empty
                ? null
                : FilteredCiphers.FirstOrDefault(c => c.Id == selectedCipherId);

            await SyncVault(cancellationToken);
            if (NetworkInformation.HasInternetAccess)
                _ = PreloadSiteIconsAsync();

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

        var selectedCipherId = SelectedCipher?.Id;

        Folders.ReplaceWith(await vaultClient.GetFoldersAsync(cancellationToken));
        await QueryCiphersAsync(cancellationToken);

        SelectedCipher = selectedCipherId == CipherId.Empty
            ? null
            : FilteredCiphers.FirstOrDefault(c => c.Id == selectedCipherId);
    }

    private Task PreloadSiteIconsAsync()
    {
        var urls = FilteredCiphers
            .OfType<LoginVaultCipher>()
            .SelectMany(static c => c.Uris)
            .Select(static u => u.TryGetWebUri(out var uri) ? uri : null)
            .Where(static uri => uri is not null)
            .Cast<Uri>()
            .ToList();

        return siteIconCache.PreloadAsync(urls);
    }
}
