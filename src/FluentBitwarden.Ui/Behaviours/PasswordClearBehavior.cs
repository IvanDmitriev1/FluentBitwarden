using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Ui.Controls.Behaviours;

[DependencyProperty<int>("ResetRequest")]
public sealed partial class PasswordClearBehavior : Behavior<PasswordBoxEx>
{
    private PasswordBoxEx? _passwordBox;

    protected override void OnAttached()
    {
        base.OnAttached();

        _passwordBox = AssociatedObject;
        _passwordBox.Unloaded += OnUnloaded;
    }

    protected override void OnDetaching()
    {
        if (_passwordBox is not null)
        {
            _passwordBox.Unloaded -= OnUnloaded;
            _passwordBox.ClearPassword();
            _passwordBox = null;
        }

        base.OnDetaching();
    }

    partial void OnResetRequestChanged() => _passwordBox?.ClearPassword();

    private void OnUnloaded(object sender, RoutedEventArgs e) => _passwordBox?.ClearPassword();
}
