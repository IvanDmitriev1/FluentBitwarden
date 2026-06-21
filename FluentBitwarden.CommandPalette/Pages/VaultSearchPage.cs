using BitwardenApi.Vault.Items.Contracts;

namespace FluentBitwarden.CommandPalette.Pages;

internal sealed partial class VaultSearchPage : DynamicListPage, IDisposable
{
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(2);

    private readonly AppHostClient _client;

    private IListItem[] _items = [];
    private CancellationTokenSource? _searchCancellation;
    private int _searchGeneration;

    public VaultSearchPage(AppHostClient client)
    {
        _client = client;

        Title = "FluentBitwarden logins";
        Name = "Search";
        PlaceholderText = "Search logins";
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
        var cancellation = Interlocked.Exchange(ref _searchCancellation, null);
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private void QueueSearch(string searchText)
    {
        int generation = Interlocked.Increment(ref _searchGeneration);

        var cancellation = new CancellationTokenSource();
        var previousCancellation = Interlocked.Exchange(ref _searchCancellation, cancellation);
        previousCancellation?.Cancel();
        previousCancellation?.Dispose();

        IsLoading = true;
        _ = SearchAsync(searchText, generation, cancellation.Token);
    }

    private async Task SearchAsync(
        string searchText,
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SearchDebounce, cancellationToken);

            if (!await _client.IsVaultUnlockedAsync(cancellationToken))
            {
                PublishItems(generation, [CreateUnlockItem()]);
                return;
            }

            using var searchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            searchTimeout.CancelAfter(SearchTimeout);

            var ciphers = await _client.SearchLoginsAsync(searchText, searchTimeout.Token);
            var items = ciphers
                .OfType<LoginVaultCipher>()
                .Where(static cipher => !cipher.Reprompt && !string.IsNullOrWhiteSpace(cipher.Password))
                .Select(CreateLoginItem)
                .ToArray();

            PublishItems(
                generation,
                items.Length == 0 ? [CreateNoResultsItem()] : items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            PublishItems(generation, [CreateSearchErrorItem()]);
        }
    }

    private IListItem CreateLoginItem(LoginVaultCipher cipher)
    {
        var copyPassword = new CopyVaultValueCommand(
            _client,
            cipher.Password!,
            "Password");

        var item = new ListItem(copyPassword)
        {
            Title = cipher.Name,
            Subtitle = GetSubtitle(cipher),
            Icon = Icons.Application,
        };

        if (!string.IsNullOrWhiteSpace(cipher.Username))
        {
            var copyUsername = new CopyVaultValueCommand(
                _client,
                cipher.Username,
                "Username");
            item.MoreCommands =
            [
                new CommandContextItem(copyUsername)
                {
                    Title = "Copy username",
                },
            ];
        }

        return item;
    }

    private static ListItem CreateUnlockItem() => new(new OpenUnlockCommand())
    {
        Title = "Unlock FluentBitwarden",
        Subtitle = "Open the app to unlock your vault, then retry",
        Icon = Icons.Unlock,
    };

    private static ListItem CreateSearchErrorItem() => new(new NoOpCommand())
    {
        Title = "Could not search FluentBitwarden",
        Subtitle = "Try the search again",
    };

    private static ListItem CreateNoResultsItem() => new(new NoOpCommand())
    {
        Title = "No matching logins",
        Subtitle = "Try a different search",
    };

    private void PublishItems(int generation, IListItem[] items)
    {
        if (generation != Volatile.Read(ref _searchGeneration))
            return;

        Volatile.Write(ref _items, items);
        IsLoading = false;
        RaiseItemsChanged(items.Length);
    }

    private static string GetSubtitle(LoginVaultCipher cipher)
    {
        if (!string.IsNullOrWhiteSpace(cipher.Username))
            return cipher.Username;

        return cipher.Uris.FirstOrDefault(static uri => !string.IsNullOrWhiteSpace(uri))
            ?? "Login";
    }
}
