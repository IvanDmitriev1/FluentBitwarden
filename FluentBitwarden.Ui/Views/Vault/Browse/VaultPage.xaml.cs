namespace FluentBitwarden.Views.Vault.Browse;

public sealed partial class VaultPage : LifecyclePage
{
    public VaultPage(VaultPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public VaultPageViewModel ViewModel { get; }
}
