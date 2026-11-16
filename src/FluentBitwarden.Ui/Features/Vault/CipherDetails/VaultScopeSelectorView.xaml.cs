using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentBitwarden.Ui.Features.Vault.ScopeSelector;

[DependencyProperty<string>("FolderName")]
[DependencyProperty<string>("AccountName")]
public sealed partial class VaultScopeSelectorView : UserControl
{
    private readonly ResourceLoader _resourceLoader = new();

    public VaultScopeSelectorView() => InitializeComponent();

    private string GetLocationName(string? folderName) =>
        string.IsNullOrEmpty(folderName) ? _resourceLoader.GetString("VaultScopePersonalVault") : folderName;
}
