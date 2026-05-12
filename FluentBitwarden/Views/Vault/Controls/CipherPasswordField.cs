using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Views.Vault.Controls;

[DependencyProperty<string>("Password", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("DisplayText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneWay)]
public sealed partial class CipherPasswordField : CipherFieldControlBase
{
    private const string MaskPassword = "●●●●●●●●●●";

    private bool _isRevealed;

    public CipherPasswordField()
    {
        DefaultStyleKey = typeof(CipherPasswordField);
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