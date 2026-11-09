using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Ui.Controls.Behaviours;

public sealed class ObservableRecipientBehavior : Behavior<FrameworkElement>
{
    private ObservableRecipient? _recipient;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Unloaded += OnUnloaded;
        AssociatedObject.DataContextChanged += OnDataContextChanged;
        UpdateRecipient();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Unloaded -= OnUnloaded;
        AssociatedObject.DataContextChanged -= OnDataContextChanged;
        DeactivateRecipient();

        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateRecipient();

    private void OnUnloaded(object sender, RoutedEventArgs e) => DeactivateRecipient();

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => UpdateRecipient();

    private void UpdateRecipient()
    {
        ObservableRecipient? recipient = AssociatedObject.IsLoaded
            ? AssociatedObject.DataContext as ObservableRecipient
            : null;

        if (ReferenceEquals(_recipient, recipient))
            return;

        DeactivateRecipient();
        if (recipient is null)
            return;

        _recipient = recipient;
        recipient.IsActive = true;
    }

    private void DeactivateRecipient()
    {
        ObservableRecipient? recipient = _recipient;
        _recipient = null;
        if (recipient is not null)
            recipient.IsActive = false;
    }
}
