using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Ui.Features.Vault.Intents;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Shell.Controls;

public sealed partial class VaultCipherSearchBoxViewModel(
    IVaultCipherClient vaultClient,
    ILogger<VaultCipherSearchBoxViewModel> logger,
    INavigation navigation) : ObservableObject
{
    [ObservableProperty]
    public partial Cipher[] Suggestions { get; set; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    private const int MaxVisibleItems = 8;

    private CancellationTokenSource? _searchCancellationTokenSource;
    private int _searchRequestId;

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            InvalidatePendingSearch();
            Suggestions = [];
            return;
        }

        SearchAsync(value).SafeFireAndForget();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Search failures must be contained so the shell remains usable.")]
    private async Task SearchAsync(string text)
    {
        InvalidatePendingSearch();
        Suggestions = [];

        string searchText = text.Trim();
        if (string.IsNullOrWhiteSpace(searchText))
            return;

        var cts = new CancellationTokenSource();
        _searchCancellationTokenSource = cts;
        int requestId = _searchRequestId;
        CancellationToken cancellationToken = cts.Token;

        try
        {
            var ciphers = await vaultClient.SearchCiphersAsync(
                new VaultCipherQuery
                {
                    SearchText = searchText,
                    Limit = MaxVisibleItems,
                },
                cancellationToken);

            if (cancellationToken.IsCancellationRequested || requestId != _searchRequestId)
                return;

            Suggestions = ciphers;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Vault cipher search failed. Exception type: {ExceptionType}",
                exception.GetType().Name);
        }
        finally
        {
            if (ReferenceEquals(_searchCancellationTokenSource, cts))
                _searchCancellationTokenSource = null;

            cts.Dispose();
        }
    }

    [RelayCommand]
    private void CancelPendingSearch()
    {
        InvalidatePendingSearch();
        Suggestions = [];
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void VaultCipherSelected(AutoSuggestBoxQuerySubmittedEventArgs? args)
    {
        if (args?.ChosenSuggestion is not Cipher cipher)
            return;

        InvalidatePendingSearch();
        Suggestions = [];
        SearchText = string.Empty;

        navigation.Child?.Navigate<VaultPage, VaultCipherNavigationIntent>(
            new ShowVaultCipherIntent(args.QueryText, cipher.Id));
    }

    private void InvalidatePendingSearch()
    {
        ++_searchRequestId;

        var cts = _searchCancellationTokenSource;
        _searchCancellationTokenSource = null;
        cts?.Cancel();
    }
}
