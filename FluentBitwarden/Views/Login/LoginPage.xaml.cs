using FluentBitwarden.ViewModels.Login;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views.Login;

public sealed partial class LoginPage : Page
{
    public LoginPage(LoginPageViewModel vm)
    {
        ViewModel = vm;
        DataContext = vm;
        InitializeComponent();
    }

    public LoginPageViewModel ViewModel { get; }
}