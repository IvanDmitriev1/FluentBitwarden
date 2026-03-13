using FluentBitwarden.ViewModels.Vault;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPage : Page
{
    public VaultPage(VaultPageViewModel vm)
    {
        ViewModel = vm;
        DataContext = vm;
        InitializeComponent();
    }

    public VaultPageViewModel ViewModel { get; }
}