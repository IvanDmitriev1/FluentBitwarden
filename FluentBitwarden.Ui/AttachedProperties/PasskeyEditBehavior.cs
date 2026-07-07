using BitwardenApi.Vault.Items.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.AttachedProperties;

/// <summary>
/// Removes the passkey from the bound <see cref="LoginVaultCipher"/> when the attached button is
/// clicked, and collapses the given row. The contract type raises no change notification, so the
/// row is hidden imperatively rather than through a binding.
/// </summary>
[AttachedDependencyProperty<FrameworkElement>("RemovePanel")]
public static partial class PasskeyEditBehavior
{
    static partial void OnRemovePanelChanged(DependencyObject dependencyObject, FrameworkElement? newValue)
    {
        if (dependencyObject is not ButtonBase button)
            return;

        button.Click -= OnClick;
        if (newValue is not null)
            button.Click += OnClick;
    }

    private static void OnClick(object sender, RoutedEventArgs e)
    {
        var button = (ButtonBase)sender;

        if (button.DataContext is LoginVaultCipher login)
            login.Fido2Credential = null;

        if (GetRemovePanel(button) is { } panel)
            panel.Visibility = Visibility.Collapsed;
    }
}
