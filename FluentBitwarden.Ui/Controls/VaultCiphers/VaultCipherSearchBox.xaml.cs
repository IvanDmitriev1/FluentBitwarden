using System.Windows.Input;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<ICommand>("VaultCipherSelectedCommand")]
public sealed partial class VaultCipherSearchBox : UserControl
{
    public sealed record Selection(string QueryText, VaultCipher SelectedItem);

    public VaultCipherSearchBox()
    {
        InitializeComponent();

        _vaultClient = App.Current.GetRequiredService<IVaultClient>();
    }

    private readonly IVaultClient _vaultClient;

    private async void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is not AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        if (string.IsNullOrWhiteSpace(sender.Text))
        {
            sender.ItemsSource = Array.Empty<VaultCipher>();
            return;
        }

        try
        {
            var ciphers = await _vaultClient.SearchCiphersAsync(new VaultCipherQuery()
            {
                SearchText = sender.Text,
                Limit = 8
            });

            sender.ItemsSource = ciphers;
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
        }
    }

    private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not VaultCipher vaultCipher)
            return;

        sender.Text = string.Empty;

        var selection = new Selection(args.QueryText, vaultCipher);
        VaultCipherSelectedCommand?.Execute(selection);
    }
}
