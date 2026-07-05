using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Controls.Shared;

[TemplateVisualState(Name = StateNoLabel, GroupName = GroupLabelStates)]
[TemplateVisualState(Name = StateHasLabel, GroupName = GroupLabelStates)]
[DependencyProperty<string>("Label")]
public sealed partial class DividedCardItemsControl : ItemsControl
{
    private const string GroupLabelStates = "LabelStates";
    private const string StateNoLabel = "NoLabel";
    private const string StateHasLabel = "HasLabel";

    public DividedCardItemsControl()
    {
        DefaultStyleKey = typeof(DividedCardItemsControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateLabelState(useTransitions: false);
    }

    partial void OnLabelChanged() => UpdateLabelState();

    private void UpdateLabelState(bool useTransitions = true)
    {
        VisualStateManager.GoToState(
            this,
            string.IsNullOrWhiteSpace(Label) ? StateNoLabel : StateHasLabel,
            useTransitions);
    }
}
