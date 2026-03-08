using FluentBitwarden.Ui.Controls;
using FluentBitwarden.ViewModels;

namespace FluentBitwarden.Views;

public sealed partial class LoginPage : CorePage
{
    public LoginPage(LoginPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public LoginPageViewModel ViewModel { get; }
}
