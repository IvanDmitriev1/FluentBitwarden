using FluentBitwarden.CommandPalette.VaultListItems;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Pages;

internal sealed partial class VaultSearchPage : DynamicListPage, IDisposable
{
    public const string PageId = "vault-search";

    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(2);

    private readonly IAccountsClient _accountsClient;
    private readonly IVaultClient _vaultClient;
    private readonly UnlockVaultPage _unlockVaultPage;
    private readonly VaultCipherListItemFactory _vaultCipherListItemFactory;
    private readonly IDisposable _sessionStatusSubscription;

    private IListItem[] _items = [];
    private CancellationTokenSource? _searchCancellation;
    private uint _searchGeneration;

    public VaultSearchPage(
        IAccountsClient accountsClient,
        IVaultClient vaultClient,
        IIpcEventClient eventClient,
        UnlockVaultPage unlockVaultPage,
        VaultCipherListItemFactory vaultCipherListItemFactory)
    {
        _accountsClient = accountsClient;
        _vaultClient = vaultClient;
        _unlockVaultPage = unlockVaultPage;
        _vaultCipherListItemFactory = vaultCipherListItemFactory;
        _sessionStatusSubscription = eventClient.Subscribe<VaultSessionStatusChangedEvent>(OnSessionStatusChanged);

        Id = PageId;
        Title = "FluentBitwarden vault";
        Name = "Search";
        PlaceholderText = "Search vault";
        Icon = Icons.Application;
        IsLoading = true;

        QueueSearch(string.Empty);
    }

    public override IListItem[] GetItems() => Volatile.Read(ref _items);

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (StringComparer.Ordinal.Equals(oldSearch, newSearch))
            return;

        QueueSearch(newSearch);
    }

    public void Dispose()
    {
        _sessionStatusSubscription.Dispose();

        var cancellation = Interlocked.Exchange(ref _searchCancellation, null);
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private void OnSessionStatusChanged(VaultSessionStatusChangedEvent message)
    {
        QueueSearch(string.Empty);
    }

    private void QueueSearch(string searchText)
    {
        uint generation = Interlocked.Increment(ref _searchGeneration);

        var cancellation = new CancellationTokenSource();
        var previousCancellation = Interlocked.Exchange(ref _searchCancellation, cancellation);
        previousCancellation?.Cancel();
        previousCancellation?.Dispose();

        IsLoading = true;
        _ = SearchAsync(searchText, generation, cancellation.Token);
    }

    private async Task SearchAsync(
        string searchText,
        uint generation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SearchDebounce, cancellationToken);

            if (await _accountsClient.GetUnlockedAccount(cancellationToken) is null)
            {
                ListItem unlockListItem = new(_unlockVaultPage)
                {
                    Title = "Unlock vault",
                    Subtitle = "Vault is locked. Unlock before searching.",
                    Icon = Icons.Unlock
                };

                PublishItems(generation, [unlockListItem]);
                return;
            }

            using var searchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            searchTimeout.CancelAfter(SearchTimeout);

            var query = new VaultCipherQuery
            {
                SearchText = searchText,
                Limit = 50,
                SortField = VaultCipherSortField.Name,
                SortDirection = VaultCipherSortDirection.Ascending,
            };

            var ciphers = await _vaultClient.SearchCiphersAsync(query, searchTimeout.Token);
            if (ciphers.Length == 0)
            {
                PublishItems(generation, [
                    new ListItem(new NoOpCommand())
                    {
                        Title = "No matching items",
                        Subtitle = "Try a different search"
                    }
                ]);
            }

            var pushItems = ciphers
                .Select(_vaultCipherListItemFactory.Create)
                .ToArray();

            PublishItems(generation, pushItems);
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception)
        {
            var errorItem = new ListItem(new NoOpCommand())
            {
                Title = "Could not search FluentBitwarden",
                Subtitle = "Try the search again"
            };
            PublishItems(generation, [errorItem]);
        }
    }

    private void PublishItems(uint generation, IListItem[] items)
    {
        if (generation != Volatile.Read(ref _searchGeneration))
            return;

        Volatile.Write(ref _items, items);
        IsLoading = false;
        RaiseItemsChanged(items.Length);
    }
}
