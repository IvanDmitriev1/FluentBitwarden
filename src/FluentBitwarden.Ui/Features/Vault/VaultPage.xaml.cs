namespace FluentBitwarden.Ui.Features.Vault;

public sealed partial class VaultPage : Page
{
    public VaultPage()
    {
        InitializeComponent();

        ViewModel = App.Current.GetRequiredService<VaultPageViewModel>();
    }

    public VaultPageViewModel ViewModel { get; }
}
