using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Ui.Behaviours;

public sealed class ObservableRecipientBehavior : Behavior<FrameworkElement>
{
    private ObservableRecipient? _recipient;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Unloaded += OnUnloaded;
        ActivateRecipient();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Unloaded -= OnUnloaded;
        DeactivateRecipient();

        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ActivateRecipient();

    private void OnUnloaded(object sender, RoutedEventArgs e) => DeactivateRecipient();

    private void ActivateRecipient()
    {
        _recipient ??= AssociatedObject.DataContext as ObservableRecipient;
        _recipient?.IsActive = true;
    }

    private void DeactivateRecipient()
    {
        ObservableRecipient? recipient = _recipient;
        _recipient = null;
        recipient?.IsActive = false;
    }
}
