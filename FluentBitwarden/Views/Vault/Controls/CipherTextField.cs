using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Views.Vault.Controls;

[DependencyProperty<string>("Text")]
public sealed partial class CipherTextField : CipherFieldControlBase
{
    public CipherTextField()
    {
        DefaultStyleKey = typeof(CipherTextField);
    }


    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override void OnPrimaryAction() => CopyTextToClipboard(Text);
}