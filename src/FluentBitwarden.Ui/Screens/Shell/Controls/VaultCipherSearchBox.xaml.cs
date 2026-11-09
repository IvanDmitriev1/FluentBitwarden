using System.Windows.Input;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Screens.Shell.Models;

namespace FluentBitwarden.Ui.Screens.Shell.Controls;

[DependencyProperty<ICommand>("VaultCipherSelectedCommand")]
[DependencyProperty<int>("MaxVisibleItems", DefaultValue = 8)]
public sealed partial class VaultCipherSearchBox : UserControl
{
    public VaultCipherSearchBox()
    {
        InitializeComponent();

        _vaultClient = App.Current.GetRequiredService<IVaultCipherClient>();
        //TODO _logger = App.Current.GetRequiredService<ILogger<VaultCipherSearchBox>>();
    }

    private readonly IVaultCipherClient _vaultClient;

    private CancellationTokenSource? _searchCancellationTokenSource;
    private int _searchRequestId;



    private void VaultCipherSearchBox_OnUnloaded(object sender, RoutedEventArgs e)
    {
        CancelPendingSearch();
        SearchBox.ItemsSource = Array.Empty<Cipher>();
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Exceptions must be handled in async void UI event handlers.")]

    private async void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is not AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        CancelPendingSearch();
        sender.ItemsSource = Array.Empty<Cipher>();
        string searchText = sender.Text.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
            return;

        var cts = new CancellationTokenSource();
        _searchCancellationTokenSource = cts;

        int requestId = _searchRequestId;
        CancellationToken cancellationToken = cts.Token;

        try
        {
            var ciphers = await _vaultClient.SearchCiphersAsync(
                new VaultCipherQuery
                {
                    SearchText = searchText,
                    Limit = MaxVisibleItems
                },
                cancellationToken);

            if (cancellationToken.IsCancellationRequested
                || requestId != _searchRequestId
                || !IsLoaded
                || !string.Equals(
                    searchText,
                    sender.Text.Trim(),
                    StringComparison.Ordinal))
            {
                return;
            }

            sender.ItemsSource = ciphers;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (Exception)
        {
            //TODO log
        }
        finally
        {
            if (ReferenceEquals(_searchCancellationTokenSource, cts))
                _searchCancellationTokenSource = null;

            cts.Dispose();
        }
    }

    private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not Cipher cipher)
            return;

        var selection = new VaultCipherSelection(
            args.QueryText,
            cipher);

        if (VaultCipherSelectedCommand?.CanExecute(selection) != true)
            return;

        CancelPendingSearch();

        sender.Text = string.Empty;
        sender.ItemsSource = Array.Empty<Cipher>();

        VaultCipherSelectedCommand.Execute(selection);
    }

    private void CancelPendingSearch()
    {
        ++_searchRequestId;

        var cts = _searchCancellationTokenSource;
        _searchCancellationTokenSource = null;

        cts?.Cancel();
    }
}
