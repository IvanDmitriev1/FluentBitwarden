using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace FluentBitwarden.AttachedProperties;

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
        {
            return;
        }

        bool focus = Target.Focus(FocusState.Programmatic);
        if (focus)
        {
            args.Handled = true;
        }
    }
}