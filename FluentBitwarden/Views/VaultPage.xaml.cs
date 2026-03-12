using FluentBitwarden.Models.Navigation;
using FluentBitwarden.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Views;

public sealed partial class VaultPage : Page
{
    public VaultPage(VaultPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public VaultPageViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.SetNavigationContext(e.Parameter as VaultNavigationContext);
    }
}
