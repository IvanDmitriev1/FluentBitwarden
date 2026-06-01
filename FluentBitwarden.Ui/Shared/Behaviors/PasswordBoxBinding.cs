using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Shared.Behaviors;

[AttachedDependencyProperty<bool, PasswordBox>("BindPassword")]
[AttachedDependencyProperty<string, PasswordBox>("BoundPassword", DefaultBindingMode = DefaultBindingMode.TwoWay)]
public static partial class PasswordBoxBinding
{
    private sealed class BindingState
    {
        public bool IsUpdating { get; set; }
    }

    private static readonly ConditionalWeakTable<PasswordBox, BindingState> States = [];

    static partial void OnBindPasswordChanged(PasswordBox passwordBox, bool oldValue, bool newValue)
    {
        passwordBox.PasswordChanged -= OnPasswordChanged;

        if (newValue)
        {
            passwordBox.PasswordChanged += OnPasswordChanged;
            SyncPasswordBox(passwordBox, PasswordBoxBinding.GetBoundPassword(passwordBox) ?? string.Empty);
        }
    }

    static partial void OnBoundPasswordChanged(PasswordBox passwordBox, string? oldValue, string? newValue)
    {
        if (PasswordBoxBinding.GetBindPassword(passwordBox) && !GetState(passwordBox).IsUpdating)
        {
            SyncPasswordBox(passwordBox, newValue ?? string.Empty);
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        BindingState state = GetState(passwordBox);
        state.IsUpdating = true;
        SetBoundPassword(passwordBox, passwordBox.Password);
        state.IsUpdating = false;
    }

    private static void SyncPasswordBox(PasswordBox passwordBox, string value)
    {
        if (!string.Equals(passwordBox.Password, value, StringComparison.Ordinal))
        {
            passwordBox.Password = value;
        }
    }

    private static BindingState GetState(PasswordBox passwordBox)
        => States.GetOrCreateValue(passwordBox);
}