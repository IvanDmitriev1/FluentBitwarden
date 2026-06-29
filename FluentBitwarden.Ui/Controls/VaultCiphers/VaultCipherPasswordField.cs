using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<string>("Password", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("DisplayText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneWay)]
public sealed partial class VaultCipherPasswordField : VaultCipherFieldControlBase
{
    private const string MaskPassword = "●●●●●●●●●●";

    private bool _isRevealed;

    public VaultCipherPasswordField()
    {
        DefaultStyleKey = typeof(VaultCipherPasswordField);
    }

    partial void OnPasswordChanged()
    {
        DisplayText = MaskPassword;
    }

    protected override FlyoutBase? CreateMenuFlyout()
    {
        var flyout = new MenuFlyout();

        var revealItem = new MenuFlyoutItem
        {
            Text = _isRevealed ? "Conceal" : "Reveal"
        };

        revealItem.Click += (_, _) =>
        {
            _isRevealed = !_isRevealed;
            DisplayText = _isRevealed
                ? Password ?? string.Empty
                : MaskPassword;
        };

        flyout.Items.Add(revealItem);
        return flyout;
    }

    protected override void OnPrimaryAction()
    {
        CopyTextToClipboard(Password);
    }
}
