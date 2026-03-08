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

    internal LoginPageViewModel ViewModel { get; }
}
