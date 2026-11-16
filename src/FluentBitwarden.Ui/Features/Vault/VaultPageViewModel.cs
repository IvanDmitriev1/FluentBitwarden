using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Organizations;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Settings.Composition;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using Navis.WinUI.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Ui.Features.Vault;

public sealed partial class VaultPageViewModel(
    IVaultCipherClient vaultClient,
    ICompositeSettingsStore settingsStore,
    ILogger<VaultPageViewModel> logger,
    IVaultFolderClient folderClient,
    IVaultOrganizationClient organizationClient,
    IAccountClient accountClient) : ObservableObject, INavigationAware, INavigationAware<VaultCipherNavigationIntent>
{
    private AccountProfileDetails? _account;
    private Folder[] _folders = [];
    private bool _suppressSearchRefresh;
    private long _loadVersion;
    private CipherType? _category;
    private VaultCipherSortDirection _sortDirection = VaultCipherSortDirection.Ascending;

    [ObservableProperty]
    public partial Cipher[] FilteredCiphers { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PersonalVaultAccountName))]
    [NotifyPropertyChangedFor(nameof(IsPersonalVaultItem))]
    public partial Cipher? SelectedCipher { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? OrganizationName { get; private set; }

    public bool IsPersonalVaultItem => SelectedCipher is { OrganizationId.IsEmpty: true };

    public string? PersonalVaultAccountName => IsPersonalVaultItem ? _account?.Name : null;

    [ObservableProperty]
    public partial string? FolderName { get; private set; }

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken) =>
        BrowseAsync(null, cancellationToken);

    public ValueTask OnNavigatedToAsync(
        VaultCipherNavigationIntent parameter,
        CancellationToken cancellationToken)
    {
        return parameter is ShowVaultCipherIntent intent
            ? BrowseAsync(intent, cancellationToken)
            : throw new ArgumentOutOfRangeException(nameof(parameter), "Invalid navigation intent.");
    }

    private async ValueTask BrowseAsync(ShowVaultCipherIntent? intent, CancellationToken cancellationToken)
    {
        var version = ++_loadVersion;
        if (version != _loadVersion || cancellationToken.IsCancellationRequested)
            return;

        if (_account is null)
        {
            _account = await accountClient.GetActiveAccountProfileDetailsAsync(default);
            OnPropertyChanged(nameof(PersonalVaultAccountName));
        }

        if (_folders.Length == 0)
        {
            var folders = await folderClient.GetFoldersAsync(default, cancellationToken);
            _folders = folders;
        }

        if (intent is not null)
        {
            await NavigateToCoreAsync(intent.SearchText, intent.CipherId, cancellationToken);
            return;
        }

        var state = settingsStore.GetComposite(UiSettingKeys.Vault.StateKey);
        var loaded = await NavigateToCoreAsync(state.SearchText, state.SelectedCipherId, cancellationToken);
        if (loaded && !state.SelectedCipherId.IsEmpty && SelectedCipher is null)
            settingsStore.RemoveComposite(UiSettingKeys.Vault.StateKey);
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

        RefreshAsync(value)
            .SafeFireAndForget();
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
        FolderName = _folders is not null && newValue is { FolderId.IsEmpty: false }
            ? Array.Find(_folders, folder => folder.Id == newValue.FolderId)?.Name
            : null;

        if (oldValue?.OrganizationId != newValue?.OrganizationId)
        {
            OrganizationName = null;
            if (newValue is { OrganizationId.IsEmpty: false })
            {
                LoadOrganizationNameAsync(newValue.OrganizationId)
                    .SafeFireAndForget();
            }
        }

        if (newValue is { Id.IsEmpty: false } && oldValue?.Id != newValue.Id)
            settingsStore.SetComposite(UiSettingKeys.Vault.StateKey, new VaultBrowseState(SearchText, newValue.Id));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Organization labels are optional presentation data.")]
    private async Task LoadOrganizationNameAsync(OrganizationId organizationId)
    {
        try
        {
            var response = await organizationClient.GetOrganizationNameAsync(new(organizationId));
            if (SelectedCipher?.OrganizationId == organizationId)
                OrganizationName = response.Name;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Vault organization lookup failed. Exception type: {ExceptionType}", exception.GetType().Name);
        }
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
            return version != _loadVersion ? null : results;
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
