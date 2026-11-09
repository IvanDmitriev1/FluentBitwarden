using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Ui.Controls.Behaviours;

[DependencyProperty<bool>("FocusOnLoad")]
[DependencyProperty<int>("FocusRequest")]
public sealed partial class FocusBehavior : Behavior<Control>
{
    private bool _focusPending;
    private int _lastFocusRequest;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.IsEnabledChanged += OnIsEnabledChanged;

        if (FocusOnLoad || _focusPending)
            RequestFocus();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.IsEnabledChanged -= OnIsEnabledChanged;
        base.OnDetaching();
    }

    partial void OnFocusOnLoadChanged()
    {
        if (FocusOnLoad)
            RequestFocus();
    }

    partial void OnFocusRequestChanged()
    {
        if (_lastFocusRequest == FocusRequest)
            return;

        _lastFocusRequest = FocusRequest;
        RequestFocus();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (FocusOnLoad || _focusPending)
            RequestFocus();
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_focusPending && AssociatedObject is { IsEnabled: true })
            TryFocus();
    }

    private void RequestFocus()
    {
        _focusPending = true;
        TryFocus();
    }

    private void TryFocus()
    {
        Control? control = AssociatedObject;
        if (control is null || !control.IsLoaded || !control.IsEnabled)
            return;

        _focusPending = !control.Focus(FocusState.Programmatic);
    }
}
