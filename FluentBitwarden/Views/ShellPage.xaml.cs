using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();

        Nav.SelectedItem = Nav.MenuItems[0];
        ContentFrame.Navigate(typeof(VaultPage));

    }
}