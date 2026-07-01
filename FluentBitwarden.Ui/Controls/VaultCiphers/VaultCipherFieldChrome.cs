using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;


[TemplatePart(Name = PartPrimaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartSecondaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionTextBlock, Type = typeof(TextBlock))]
[DependencyProperty<string>("Label", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("ActionText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<Visibility>("SecondaryButtonVisibility", DefaultValue = Visibility.Collapsed)]
public sealed partial class VaultCipherFieldChrome : SplitButton
{
    private const string PartPrimaryButton = "PrimaryButton";
    private const string PartSecondaryButton = "SecondaryButton";
    private const string PartActionTextBlock = "PART_ActionTextBlock";

    private Button? _secondaryButton;
    private readonly DependencyPropertyCallbackRegistration _flyoutCallbackRegistration;

    public VaultCipherFieldChrome()
    {
        DefaultStyleKey = typeof(VaultCipherFieldChrome);
        _flyoutCallbackRegistration = new DependencyPropertyCallbackRegistration(
            this,
            FlyoutProperty,
            static (sender, dp) => ((VaultCipherFieldChrome)sender).OnFlyoutChanged(dp));
    }

    protected override void OnApplyTemplate()
    {
        _flyoutCallbackRegistration.Unregister();

        if (_secondaryButton is not null)
        {
            _secondaryButton.Flyout = null;
        }

        base.OnApplyTemplate();

        _secondaryButton = GetTemplateChild(PartSecondaryButton) as Button;
        _flyoutCallbackRegistration.Register();
        UpdateFlyoutState();
    }

    private void OnFlyoutChanged(DependencyProperty dp)
    {
        UpdateFlyoutState();
    }

    private void UpdateFlyoutState()
    {
        var flyout = Flyout;
        SecondaryButtonVisibility = flyout is null
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (flyout is not null)
        {
            flyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;
        }

        if (_secondaryButton is not null)
        {
            _secondaryButton.Flyout = flyout;
        }
    }
}
