using FluentBitwarden.ViewModels.Passkeys;

namespace FluentBitwarden.Views.Passkeys;

public sealed partial class PasskeySelectPage : Page
{
    public PasskeySelectPage(PasskeySelectPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public PasskeySelectPageViewModel ViewModel { get; }
}