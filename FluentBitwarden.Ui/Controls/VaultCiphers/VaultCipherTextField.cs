using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<string>("Text")]
public sealed partial class VaultCipherTextField : VaultCipherFieldControlBase
{
    public VaultCipherTextField()
    {
        DefaultStyleKey = typeof(VaultCipherTextField);
    }


    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override void OnPrimaryAction() => CopyTextToClipboard(Text);
}
