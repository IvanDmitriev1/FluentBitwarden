using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Resources.Controls;


[TemplatePart(Name = PartTapTarget, Type = typeof(Button))]
[TemplatePart(Name = PartActionText, Type = typeof(Button))]
[TemplatePart(Name = PartMenuButton, Type = typeof(Button))]
[DependencyProperty<string>("Label", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("ActionText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<FlyoutBase>("MenuFlyout", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<Visibility>("MenuButtonVisibility", DefaultValue = Visibility.Collapsed)]
public sealed partial class CipherFieldChrome : Button
{
    private const string PartTapTarget = "PART_TapTarget";
    private const string PartActionText = "PART_ActionText";
    private const string PartMenuButton = "PART_MenuButton";

    public CipherFieldChrome()
    {
        DefaultStyleKey = typeof(CipherFieldChrome);
    }

    partial void OnMenuFlyoutChanged()
    {
        MenuButtonVisibility = MenuFlyout is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}