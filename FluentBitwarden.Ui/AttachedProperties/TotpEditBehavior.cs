using System.Text;
using BitwardenApi.Vault.Items.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.AttachedProperties;

/// <summary>
/// Two-way edits a <see cref="TotpValue"/> through a <see cref="TextBox"/>: displays the stored
/// secret string, and on lost focus parses the text back into the bound value (empty clears it,
/// invalid input reverts to the last valid secret).
/// </summary>
[AttachedDependencyProperty<TotpValue>("Source")]
public static partial class TotpEditBehavior
{
    static partial void OnSourceChanged(DependencyObject dependencyObject, TotpValue? newValue)
    {
        if (dependencyObject is not TextBox textBox)
            return;

        textBox.LostFocus -= OnLostFocus;
        textBox.LostFocus += OnLostFocus;

        // Do not clobber the text the user is actively editing.
        if (textBox.FocusState == FocusState.Unfocused)
            textBox.Text = newValue?.ToStorageString() ?? string.Empty;
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var text = textBox.Text.Trim();

        if (text.Length == 0)
        {
            SetSource(textBox, null);
            return;
        }

        if (TotpValue.TryParse(Encoding.UTF8.GetBytes(text), out var parsed))
            SetSource(textBox, parsed);
        else
            textBox.Text = GetSource(textBox)?.ToStorageString() ?? string.Empty;
    }
}
