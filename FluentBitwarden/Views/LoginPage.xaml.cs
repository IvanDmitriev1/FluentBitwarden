using FluentBitwarden.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views;

public sealed partial class LoginPage : Page
{
    public LoginPage(LoginPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    internal LoginPageViewModel ViewModel { get; }
}
