using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.VaultCiphers;


[TemplatePart(Name = PartPrimaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartSecondaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionTextBlock, Type = typeof(TextBlock))]
[TemplateVisualState(Name = StateNoFlyout, GroupName = GroupFlyoutStates)]
[TemplateVisualState(Name = StateHasFlyout, GroupName = GroupFlyoutStates)]
[TemplateVisualState(Name = StateNoLabel, GroupName = GroupLabelStates)]
[TemplateVisualState(Name = StateHasLabel, GroupName = GroupLabelStates)]
[DependencyProperty<string>("Label")]
[DependencyProperty<string>("ActionText")]
public sealed partial class VaultCipherFieldChrome : SplitButton
{
    private const string PartPrimaryButton = "PrimaryButton";
    private const string PartSecondaryButton = "SecondaryButton";
    private const string PartActionTextBlock = "PART_ActionTextBlock";

    private const string GroupFlyoutStates = "FlyoutAvailabilityStates";
    private const string StateNoFlyout = "NoFlyout";
    private const string StateHasFlyout = "HasFlyout";

    private const string GroupLabelStates = "LabelStates";
    private const string StateNoLabel = "NoLabel";
    private const string StateHasLabel = "HasLabel";

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
        UpdateLabelState(useTransitions: false);
    }

    partial void OnLabelChanged() => UpdateLabelState();

    private void UpdateFlyoutState(bool useTransitions = true)
    {
        VisualStateManager.GoToState(
            this,
            Flyout is null ? StateNoFlyout : StateHasFlyout,
            useTransitions);
    }

    private void UpdateLabelState(bool useTransitions = true)
    {
        VisualStateManager.GoToState(
            this,
            string.IsNullOrWhiteSpace(Label) ? StateNoLabel : StateHasLabel,
            useTransitions);
    }
}
