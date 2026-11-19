using System.Windows.Input;
using CommunityToolkit.WinUI;
using FluentBitwarden.Ui.Infrastructure;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace FluentBitwarden.Ui.Controls;

[TemplatePart(Name = PartRoot, Type = typeof(Grid))]
[TemplatePart(Name = PartPrimaryButton, Type = typeof(Button))]
[TemplatePart(Name = PartActionsButton, Type = typeof(DropDownButton))]
[TemplateVisualState(Name = "Resting", GroupName = "InteractionStates")]
[TemplateVisualState(Name = "Interacting", GroupName = "InteractionStates")]
[DependencyProperty<string>("Label")]
[DependencyProperty<ICommand>("Command")]
[DependencyProperty<object>("CommandParameter")]
[DependencyProperty<string>("ActionText", DefaultValue = "")]
[DependencyProperty<object>("SupportingContent")]
[DependencyProperty<DataTemplate>("SupportingContentTemplate")]
[DependencyProperty<FlyoutBase>("ActionsFlyout")]
public sealed partial class FieldActionRow : ContentControl
{
    private const string PartRoot = "PART_Root";
    private const string PartPrimaryButton = "PART_PrimaryButton";
    private const string PartActionsButton = "PART_ActionsButton";

    private Grid? _root;
    private Button? _primaryButton;
    private DropDownButton? _actionsButton;
    private FrameworkElement? _actionPanel;
    private FrameworkElement? _actionHint;
    private TextBlock? _heading;
    private FlyoutBase? _observedFlyout;
    private bool _pointerWithin;
    private bool _flyoutOpen;
    private readonly DependencyPropertyCallbackRegistration _enabledCallbackRegistration;

    public FieldActionRow()
    {
        DefaultStyleKey = typeof(FieldActionRow);

        _enabledCallbackRegistration = new DependencyPropertyCallbackRegistration(
            this,
            IsEnabledProperty,
            static (sender, _) => ((FieldActionRow)sender).UpdateInteraction());
    }

    protected override void OnApplyTemplate()
    {
        _enabledCallbackRegistration.Unregister();

        ObserveFlyout(null);
        base.OnApplyTemplate();

        _root = GetTemplateChild(PartRoot) as Grid;
        _primaryButton = GetTemplateChild(PartPrimaryButton) as Button;
        _actionsButton = GetTemplateChild(PartActionsButton) as DropDownButton;
        _actionPanel = GetTemplateChild("ActionPanel") as FrameworkElement;
        _actionHint = GetTemplateChild("ActionHint") as FrameworkElement;
        _heading = GetTemplateChild("Heading") as TextBlock;
        _pointerWithin = false;

        ObserveFlyout(ActionsFlyout);
        _enabledCallbackRegistration.Register();
        UpdateParts();
    }

    partial void OnActionsFlyoutChanged()
    {
        ObserveFlyout(_root is null ? null : ActionsFlyout);
        UpdateParts();
    }

    partial void OnCommandChanged() => UpdateParts();
    partial void OnLabelChanged() => UpdateParts();
    partial void OnActionTextChanged() => UpdateParts();

    private void ObserveFlyout(FlyoutBase? flyout)
    {
        if (ReferenceEquals(_observedFlyout, flyout))
            return;

        if (_observedFlyout is not null)
        {
            _observedFlyout.Opened -= OnFlyoutOpened;
            _observedFlyout.Closed -= OnFlyoutClosed;
            _observedFlyout.Hide();
        }

        _observedFlyout = flyout;
        _flyoutOpen = false;

        if (_observedFlyout is not null)
        {
            _observedFlyout.Opened += OnFlyoutOpened;
            _observedFlyout.Closed += OnFlyoutClosed;
        }
    }

    private void UpdateParts()
    {
        _heading?.Visibility = string.IsNullOrWhiteSpace(Label) ? Visibility.Collapsed : Visibility.Visible;

        if (_primaryButton is not null)
        {
            _primaryButton.IsTabStop = Command is not null;
            string name = AutomationProperties.GetName(this);
            AutomationProperties.SetName(_primaryButton,
                string.IsNullOrWhiteSpace(name) ? $"{ActionText} {Label}".Trim() : name);
        }

        _actionHint?.Visibility = Command is null || string.IsNullOrWhiteSpace(ActionText)
                ? Visibility.Collapsed : Visibility.Visible;

        _actionsButton?.Visibility = ActionsFlyout is null ? Visibility.Collapsed : Visibility.Visible;
        _actionsButton?.IsTabStop = ActionsFlyout is not null;

        _actionPanel?.Visibility = Command is null && ActionsFlyout is null
                ? Visibility.Collapsed : Visibility.Visible;

        UpdateInteraction();
    }

    private void UpdateInteraction()
    {
        bool active = IsEnabled && (_flyoutOpen || _pointerWithin || HasKeyboardFocusWithin());
        VisualStateManager.GoToState(this, active ? "Interacting" : "Resting", true);
        VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);
    }

    private bool HasKeyboardFocusWithin() =>
        XamlRoot is { } root &&
        FocusManager.GetFocusedElement(root) is UIElement { FocusState: FocusState.Keyboard } focused &&
        focused.FindAscendantOrSelf<FieldActionRow>(row => ReferenceEquals(row, this)) is not null;

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _pointerWithin = true;
        UpdateInteraction();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        var position = e.GetCurrentPoint(this).Position;
        _pointerWithin = position.X >= 0 && position.Y >= 0 &&
            position.X < ActualWidth && position.Y < ActualHeight;

        UpdateInteraction();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        DispatcherQueue.TryEnqueue(UpdateInteraction);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        DispatcherQueue.TryEnqueue(UpdateInteraction);
    }

    private void OnFlyoutOpened(object? sender, object e)
    {
        _flyoutOpen = true;
        UpdateInteraction();
    }

    private void OnFlyoutClosed(object? sender, object e)
    {
        _flyoutOpen = false;
        UpdateInteraction();
    }
}
