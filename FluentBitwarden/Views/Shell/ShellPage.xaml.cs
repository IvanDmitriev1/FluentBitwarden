using BitwardenApi.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Vault;
using FluentBitwarden.Views.Vault.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPage : Page
{
    static ShellPage()
    {
        var pageByTag = new Dictionary<string, Type>
        {
            ["vault"] = typeof(VaultPage),
        };

        PageByTag = pageByTag;
        TagByPage = pageByTag.ToDictionary(static pair => pair.Value, static pair => pair.Key);
    }

    public ShellPage(IVaultClient vaultService, IMessenger messenger)
    {
        _vaultService = vaultService;
        _messenger = messenger;
        InitializeComponent();
        ContentFrame.Navigate(typeof(VaultPage));
    }

    private readonly IVaultClient _vaultService;
    private readonly IMessenger _messenger;

    private static readonly IReadOnlyDictionary<string, Type> PageByTag;
    private static readonly IReadOnlyDictionary<Type, string> TagByPage;

    [RelayCommand]
    private void PaneToggle()
    {
        Nav.IsPaneOpen = !Nav.IsPaneOpen;
    }

    private void Nav_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var pageType = args.IsSettingsInvoked
            ? typeof(SettingsPage)
            : PageByTag.GetValueOrDefault((string)args.InvokedItemContainer!.Tag!, typeof(VaultPage));

        ContentFrame.Navigate(pageType);
    }

    private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(SettingsPage))
        {
            Nav.SelectedItem = Nav.SettingsItem;
            return;
        }

        if (!TagByPage.TryGetValue(e.SourcePageType, out string? tag))
        {
            Nav.SelectedItem = null;
            return;
        }

        Nav.SelectedItem = Nav.MenuItems
            .Cast<NavigationViewItem>()
            .FirstOrDefault(item => (string)item.Tag == tag);

    }

    private void SearchKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        AutoSuggestBox.Focus(FocusState.Programmatic);
        args.Handled = true;
    }

    private async void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is not AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        if (string.IsNullOrWhiteSpace(sender.Text))
        {
            sender.ItemsSource = Array.Empty<VaultCipher>();
            return;
        }

        var ciphers = await _vaultService.SearchCiphersAsync(new VaultCipherQuery()
        {
            SearchText = sender.Text,
            Limit = 8
        });

        sender.ItemsSource = ciphers;
    }

    private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not VaultCipher vaultCipher)
            return;

        sender.Text = string.Empty;
        var message = new ShowVaultCipherMessage(args.QueryText, vaultCipher);


        if (ContentFrame.CurrentSourcePageType != typeof(VaultPage))
            ContentFrame.Navigate(typeof(VaultPage), PageNavigationParameter.From(message));
        else
            _messenger.Send(message);
    }
}