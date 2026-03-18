using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();

        Nav.SelectedItem = Nav.MenuItems[0];
        //ContentFrame.Navigate(typeof(VaultPage));
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        switch (args.SelectedItemContainer?.Tag)
        {
            case "vault":
                ContentFrame.Navigate(typeof(VaultPage));
                break;
        }
    }
}