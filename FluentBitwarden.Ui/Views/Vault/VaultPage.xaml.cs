namespace FluentBitwarden.Views.Vault;

public sealed partial class VaultPage : LifecyclePage
{
    public VaultPage()
    {
        ViewModel = App.Current.GetRequiredService<VaultPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public VaultPageViewModel ViewModel { get; }
}
