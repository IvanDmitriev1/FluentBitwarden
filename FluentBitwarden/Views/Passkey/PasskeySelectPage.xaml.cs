namespace FluentBitwarden.Views.Passkey;

public sealed partial class PasskeySelectPage : Page
{
    public PasskeySelectPage(PasskeySelectPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public PasskeySelectPageViewModel ViewModel { get; }
}