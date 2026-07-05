using System.Windows.Input;
using FluentBitwarden.Contracts.Modules.Vault;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<ICommand>("VaultCipherSelectedCommand")]
[DependencyProperty<int>("MaxVisibleItems", DefaultValue = 8)]
public sealed partial class VaultCipherSearchBox : UserControl
{
    public sealed record Selection(string QueryText, VaultCipher SelectedItem);
    private readonly IVaultClient _vaultClient;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private int _searchRequestId;

    public VaultCipherSearchBox()
    {
        InitializeComponent();

        _vaultClient = App.Current.GetRequiredService<IVaultClient>();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        CancelPendingSearch();
    }

    private async void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is not AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        CancelPendingSearch();

        string searchText = sender.Text.Trim();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            sender.ItemsSource = Array.Empty<VaultCipher>();
            return;
        }

        CancellationTokenSource cancellationTokenSource = new();
        _searchCancellationTokenSource = cancellationTokenSource;
        int requestId = ++_searchRequestId;

        try
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var ciphers = await _vaultClient.SearchCiphersAsync(new VaultCipherQuery()
            {
                SearchText = searchText,
                Limit = MaxVisibleItems
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested
                || requestId != _searchRequestId
                || !string.Equals(searchText, sender.Text.Trim(), StringComparison.Ordinal))
            {
                return;
            }

            sender.ItemsSource = ciphers;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
        }
        finally
        {
            if (ReferenceEquals(_searchCancellationTokenSource, cancellationTokenSource))
            {
                _searchCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not VaultCipher vaultCipher)
            return;

        CancelPendingSearch();
        sender.Text = string.Empty;
        sender.ItemsSource = Array.Empty<VaultCipher>();

        var selection = new Selection(args.QueryText, vaultCipher);
        VaultCipherSelectedCommand?.Execute(selection);
    }

    private void CancelPendingSearch()
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = null;
    }
}
