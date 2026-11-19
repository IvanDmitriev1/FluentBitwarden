namespace FluentBitwarden.Ui.Features.Vault.CipherDetails;

[DependencyProperty<Cipher>("Cipher")]
public sealed partial class VaultCipherDetailsView : UserControl
{
    public VaultCipherDetailsView()
    {
        InitializeComponent();
    }
}
