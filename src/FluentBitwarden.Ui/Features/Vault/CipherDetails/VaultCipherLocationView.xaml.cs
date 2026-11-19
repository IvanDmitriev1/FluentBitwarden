using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentBitwarden.Ui.Features.Vault.CipherDetails;

[DependencyProperty<string>("FolderName")]
[DependencyProperty<string>("AccountName")]
public sealed partial class VaultCipherLocationView : UserControl
{
    private readonly ResourceLoader _resourceLoader = new();

    public VaultCipherLocationView() => InitializeComponent();

    private string GetLocationName(string? folderName) =>
        string.IsNullOrEmpty(folderName) ? _resourceLoader.GetString("VaultScopePersonalVault") : folderName;
}
