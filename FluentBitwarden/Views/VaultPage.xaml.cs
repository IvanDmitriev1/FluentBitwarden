using FluentBitwarden.Ui.Controls;
using FluentBitwarden.Models.Navigation;
using FluentBitwarden.ViewModels;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Views;

public sealed partial class VaultPage : CorePage
{
    public VaultPage(VaultPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public VaultPageViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.SetNavigationContext(e.Parameter as VaultNavigationContext);
    }
}
