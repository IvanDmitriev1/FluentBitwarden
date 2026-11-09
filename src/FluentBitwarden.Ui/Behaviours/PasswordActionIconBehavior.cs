using FluentBitwarden.Ui.Controls;
using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Ui.Screens.Unlock;

[DependencyProperty<string>("EmptyGlyph", DefaultValue = "")]
[DependencyProperty<string>("NonEmptyGlyph", DefaultValue = "")]
public sealed partial class PasswordActionIconBehavior : Behavior<PasswordBoxEx>
{
    private PasswordBoxEx? _passwordBox;
    private FontIcon? _actionIcon;
    
    protected override void OnAttached()
    {
        base.OnAttached();

        _passwordBox = AssociatedObject;
        if (_passwordBox is null)
            return;

        _actionIcon = new FontIcon
        {
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            IsTextScaleFactorEnabled = false,
        };
        _passwordBox.ActionButtonContent = _actionIcon;
        _passwordBox.PasswordChanged += OnPasswordChanged;
        SyncPasswordAccentIcon(_passwordBox.Password);
    }

    protected override void OnDetaching()
    {
        if (_passwordBox is not null)
        {
            _passwordBox.PasswordChanged -= OnPasswordChanged;
            _passwordBox = null;
        }

        _actionIcon = null;
        base.OnDetaching();
    }

    partial void OnEmptyGlyphChanged() => SyncCurrentPassword();
    partial void OnNonEmptyGlyphChanged() => SyncCurrentPassword();

    private void OnPasswordChanged(PasswordBoxEx sender, string newPassword) =>
        SyncPasswordAccentIcon(newPassword);

    private void SyncCurrentPassword()
    {
        if (_passwordBox is not null)
            SyncPasswordAccentIcon(_passwordBox.Password);
    }

    private void SyncPasswordAccentIcon(string? password)
    {
        _actionIcon?.Glyph = string.IsNullOrWhiteSpace(password) ? EmptyGlyph : NonEmptyGlyph;
    }
}
