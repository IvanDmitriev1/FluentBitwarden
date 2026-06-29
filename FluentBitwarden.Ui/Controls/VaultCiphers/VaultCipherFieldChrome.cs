using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;


[TemplatePart(Name = PartChromeBorder, Type = typeof(Border))]
[TemplatePart(Name = PartActionTextBlock, Type = typeof(TextBlock))]
[TemplatePart(Name = PartMenuButton, Type = typeof(Button))]
[DependencyProperty<string>("Label", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("ActionText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<FlyoutBase>("MenuFlyout", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<Visibility>("MenuButtonVisibility", DefaultValue = Visibility.Collapsed)]
public sealed partial class VaultCipherFieldChrome : Button
{
    private const string PartChromeBorder = "ChromeBorder";
    private const string PartActionTextBlock = "ActionTextBlock";
    private const string PartMenuButton = "PART_MenuButton";

    public VaultCipherFieldChrome()
    {
        DefaultStyleKey = typeof(VaultCipherFieldChrome);
    }
    partial void OnMenuFlyoutChanged()
    {
        MenuButtonVisibility = MenuFlyout is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
