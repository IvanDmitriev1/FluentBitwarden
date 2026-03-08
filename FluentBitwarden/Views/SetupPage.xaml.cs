using FluentBitwarden.Ui.Controls;
using FluentBitwarden.ViewModels;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views;

public sealed partial class SetupPage : CorePage
{
    public SetupPage(SetupPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SetupPageViewModel ViewModel { get; }

    private void Pwd_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.MasterPassword = Pwd.Password;
    }
}
