using FluentBitwarden.Controls.Shared;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.AttachedProperties;

/// <summary>
/// Two-way binds a <see cref="PasswordBoxEx"/> to a string source. <see cref="PasswordBoxEx.Password"/>
/// has a private setter but a public <see cref="PasswordBoxEx.PasswordProperty"/> and
/// <see cref="PasswordBoxEx.PasswordChanged"/> event, so the seed is written through the DP (early
/// enough for the masked display to render) and edits are captured through the event.
/// </summary>
[AttachedDependencyProperty<string>("BoundPassword")]
public static partial class PasswordBoxEditBehavior
{
    static partial void OnBoundPasswordChanged(DependencyObject dependencyObject, string? newValue)
    {
        if (dependencyObject is not PasswordBoxEx box)
            return;

        box.PasswordChanged -= OnPasswordChanged;
        box.PasswordChanged += OnPasswordChanged;

        var value = newValue ?? string.Empty;
        if (box.Password != value)
            box.SetValue(PasswordBoxEx.PasswordProperty, value);
    }

    private static void OnPasswordChanged(PasswordBoxEx box, string password)
    {
        if (GetBoundPassword(box) != password)
            SetBoundPassword(box, password);
    }
}
