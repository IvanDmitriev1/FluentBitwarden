using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Data.Implementations;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Repositories;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        Nav.SelectedItem = Nav.MenuItems[0];
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        NavigationViewItem navItem = (NavigationViewItem)args.SelectedItem;
        Type navType = navItem.Tag switch
        {
            "vault" => typeof(VaultPage),
            _ => typeof(VaultPage)
        };

        ContentFrame.Navigate(navType);
    }
}