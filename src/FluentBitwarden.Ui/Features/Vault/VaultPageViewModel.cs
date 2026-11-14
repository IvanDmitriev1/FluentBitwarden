using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Platform.Settings.Composition;
using FluentBitwarden.Ui.Infrastructure;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Features.Vault;

public sealed partial class VaultPageViewModel(
    IVaultCipherClient vaultClient,
    ICompositeSettingsStore settingsStore,
    ILogger<VaultPageViewModel> logger) : ObservableObject, INavigationAware, INavigationAware<VaultCipherNavigationIntent>
{
    private bool _suppressSearchRefresh;
    private long _loadVersion;
    private CipherType? _category;
    private VaultCipherSortDirection _sortDirection = VaultCipherSortDirection.Ascending;

    [ObservableProperty]
    public partial Cipher[] FilteredCiphers { get; set; } = [];

    [ObservableProperty]
    public partial Cipher? SelectedCipher { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public async ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        var state = settingsStore.GetComposite(UiSettingKeys.Vault.StateKey);
        if (await NavigateToCoreAsync(state.SearchText, state.SelectedCipherId, cancellationToken)
            && !state.SelectedCipherId.IsEmpty && SelectedCipher is null)
        {
            settingsStore.RemoveComposite(UiSettingKeys.Vault.StateKey);
        }
    }

    public async ValueTask OnNavigatedToAsync(VaultCipherNavigationIntent parameter, CancellationToken cancellationToken)
    {
        if (parameter is ShowVaultCipherIntent intent)
            await NavigateToCoreAsync(intent.SearchText, intent.CipherId, cancellationToken);
    }

    private async ValueTask<bool> NavigateToCoreAsync(string searchText, CipherId selectedId, CancellationToken cancellationToken)
    {
        _suppressSearchRefresh = true;
        try
        {
            SearchText = searchText;
        }
        finally
        {
            _suppressSearchRefresh = false;
        }

        ClearResults();
        if (await LoadAsync(searchText, cancellationToken) is not { } results)
            return false;

        FilteredCiphers = results;
        if (!selectedId.IsEmpty)
            SelectedCipher = Array.Find(results, cipher => cipher.Id == selectedId);

        return true;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        _loadVersion++;
        _suppressSearchRefresh = true;
        try
        {
            SearchText = string.Empty;
        }
        finally
        {
            _suppressSearchRefresh = false;
        }

        ClearResults();

        return ValueTask.CompletedTask;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_suppressSearchRefresh)
            return;

        RefreshAsync(value).SafeFireAndForget();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task CategoryChangedAsync(CipherType? category)
    {
        _category = category;
        return RefreshAsync(SearchText);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SortChangedAsync(VaultCipherSortDirection direction)
    {
        _sortDirection = direction;
        if (await LoadAsync(SearchText) is not { } results)
            return;

        var selectedId = SelectedCipher?.Id;
        FilteredCiphers = results;
        SelectedCipher = selectedId is { } id ? Array.Find(results, cipher => cipher.Id == id) : null;
    }

    partial void OnSelectedCipherChanged(Cipher? oldValue, Cipher? newValue)
    {
        if (newValue is not null && !newValue.Id.IsEmpty && oldValue?.Id != newValue.Id)
            settingsStore.SetComposite(UiSettingKeys.Vault.StateKey, new VaultBrowseState(SearchText, newValue.Id));
    }

    private async Task RefreshAsync(string text)
    {
        ClearResults();

        if (await LoadAsync(text) is { } results)
            FilteredCiphers = results;
    }

    private void ClearResults()
    {
        FilteredCiphers = [];
        SelectedCipher = null;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Vault load failures must leave the page available for another search.")]
    private async Task<Cipher[]?> LoadAsync(string text, CancellationToken cancellationToken = default)
    {
        var version = ++_loadVersion;

        try
        {
            var query = new VaultCipherQuery
            {
                SearchText = text.Trim(),
                CipherType = _category,
                SortDirection = _sortDirection,
            };

            var results = await vaultClient.SearchCiphersAsync(query, cancellationToken);
            return cancellationToken.IsCancellationRequested || version != _loadVersion ? null : results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError("Vault list load failed. Exception type: {ExceptionType}", exception.GetType().Name);
        }

        return null;
    }

}
