using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.VaultCiphers;


[TemplatePart(Name = PartPrimaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartSecondaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionTextBlock, Type = typeof(TextBlock))]
[TemplateVisualState(Name = StateNoFlyout, GroupName = GroupStates)]
[TemplateVisualState(Name = StateHasFlyout, GroupName = GroupStates)]
[DependencyProperty<string>("Label", DefaultValue = "")]
[DependencyProperty<string>("ActionText", DefaultValue = "")]
public sealed partial class VaultCipherFieldChrome : SplitButton
{
    private const string PartPrimaryButton = "PrimaryButton";
    private const string PartSecondaryButton = "SecondaryButton";
    private const string PartActionTextBlock = "PART_ActionTextBlock";

    private const string GroupStates = "FlyoutAvailabilityStates";
    private const string StateNoFlyout = "NoFlyout";
    private const string StateHasFlyout = "HasFlyout";

    private readonly DependencyPropertyCallbackRegistration _flyoutCallbackRegistration;

    public VaultCipherFieldChrome()
    {
        DefaultStyleKey = typeof(VaultCipherFieldChrome);
        _flyoutCallbackRegistration = new DependencyPropertyCallbackRegistration(
            this,
            FlyoutProperty,
            static (sender, _) => ((VaultCipherFieldChrome)sender).UpdateFlyoutState());
    }

    protected override void OnApplyTemplate()
    {
        _flyoutCallbackRegistration.Unregister();

        base.OnApplyTemplate();

        _flyoutCallbackRegistration.Register();
        UpdateFlyoutState(useTransitions: false);
    }

    private void UpdateFlyoutState(bool useTransitions = true)
    {
        VisualStateManager.GoToState(
            this,
            Flyout is null ? StateNoFlyout : StateHasFlyout,
            useTransitions);
    }
}
