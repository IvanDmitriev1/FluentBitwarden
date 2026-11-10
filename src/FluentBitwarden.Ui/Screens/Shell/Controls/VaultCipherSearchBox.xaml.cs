namespace FluentBitwarden.Ui.Screens.Shell.Controls;

public sealed partial class VaultCipherSearchBox : UserControl
{
    public VaultCipherSearchBox()
    {
        InitializeComponent();

        ViewModel = App.Current.GetRequiredService<VaultCipherSearchBoxViewModel>();
    }

    public VaultCipherSearchBoxViewModel ViewModel { get; }
}
