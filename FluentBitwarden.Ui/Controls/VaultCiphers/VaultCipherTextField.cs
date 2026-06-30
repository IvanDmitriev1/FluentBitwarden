using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<string>("Label", DefaultValue = "")]
[DependencyProperty<string>("Text")]
[DependencyProperty<string>("ActionText")]
public partial class VaultCipherTextField : VaultCipherFieldControlBase
{
    public VaultCipherTextField()
    {
        DefaultStyleKey = typeof(VaultCipherTextField);
    }

    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override void OnPrimaryAction()
    {
        if (string.IsNullOrEmpty(ActionText))
            return;
        
        CopyTextToClipboard(Text);
    }
}
