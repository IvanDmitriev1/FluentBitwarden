using System.Linq;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPage : LifecyclePage
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

    public ShellPage(ShellPageViewModel viewModel)
    {
        DataContext = viewModel;
        ViewModel = viewModel;

        InitializeComponent();
        ContentFrame.Navigate(typeof(VaultPage));
    }

    private static readonly IReadOnlyDictionary<string, Type> PageByTag;
    private static readonly IReadOnlyDictionary<Type, string> TagByPage;


    public ShellPageViewModel ViewModel { get; }


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
}