using Microsoft.UI.Xaml.Input;

namespace FluentBitwarden.Ui.Behaviours;

[DependencyProperty<UIElement>("Target")]
public sealed partial class FocusKeyboardAccelerator : KeyboardAccelerator
{
    partial void OnTargetChanged(UIElement? newValue)
    {
        Invoked -= OnInvoked;

        if (newValue is not null)
        {
            Invoked += OnInvoked;
        }
    }

    private void OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (Target is null)
            return;

        args.Handled = Target.Focus(FocusState.Programmatic);
    }
}
