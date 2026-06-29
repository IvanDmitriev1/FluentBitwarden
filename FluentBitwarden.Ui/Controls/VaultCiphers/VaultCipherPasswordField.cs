using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<string>("Password", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<string>("DisplayText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneWay)]
public sealed partial class VaultCipherPasswordField : VaultCipherFieldControlBase
{
    private const string MaskPassword = "●●●●●●●●●●";

    private const string RevealResourceKey = "VaultCipherPasswordFieldRevealText";
    private const string ConcealResourceKey = "VaultCipherPasswordFieldConcealText";
    private static readonly string MaskPasswordText = string.IsNullOrEmpty(MaskPassword)
        ? new string('\u2022', 10)
        : new string('\u2022', 10);

    private bool _isRevealed;
    private MenuFlyout? _menuFlyout;
    private MenuFlyoutItem? _toggleRevealMenuItem;

    public VaultCipherPasswordField()
    {
        DefaultStyleKey = typeof(VaultCipherPasswordField);
    }

    partial void OnPasswordChanged()
    {
        UpdateDisplayText();
    }

    protected override FlyoutBase? CreateMenuFlyout()
    {
        if (_menuFlyout is null)
        {
            _toggleRevealMenuItem = new MenuFlyoutItem();
            _toggleRevealMenuItem.Click += OnToggleRevealMenuItemClick;

            _menuFlyout = new MenuFlyout();
            _menuFlyout.Items.Add(_toggleRevealMenuItem);
        }

        UpdateRevealMenuItemText();
        return _menuFlyout;
    }

    protected override void OnPrimaryAction()
    {
        CopyTextToClipboard(Password);
    }

    private void OnToggleRevealMenuItemClick(object sender, RoutedEventArgs e)
    {
        _isRevealed = !_isRevealed;
        UpdateDisplayText();
        UpdateRevealMenuItemText();
    }

    private void UpdateDisplayText()
    {
        DisplayText = _isRevealed
            ? Password ?? string.Empty
            : string.IsNullOrEmpty(Password)
                ? string.Empty
                : MaskPasswordText;
    }

    private void UpdateRevealMenuItemText()
    {
        if (_toggleRevealMenuItem is null)
            return;

        string key = _isRevealed ? ConcealResourceKey : RevealResourceKey;
        string fallback = _isRevealed ? "Conceal" : "Reveal";

        _toggleRevealMenuItem.Text = GetResourceString(key, fallback);
    }

    private static string GetResourceString(string key, string fallback)
    {
        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(key, out object value)
            && value is string resourceText)
        {
            return resourceText;
        }

        return fallback;
    }
}
