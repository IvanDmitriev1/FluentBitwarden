namespace FluentBitwarden.Views.Passkeys.CredentialSelection;

public sealed partial class PasskeySelectPage : Page
{
    public PasskeySelectPage(PasskeySelectPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public PasskeySelectPageViewModel ViewModel { get; }
}