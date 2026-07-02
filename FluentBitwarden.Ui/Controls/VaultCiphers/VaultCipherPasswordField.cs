using FluentBitwarden.Platform.Infrastructure.Clipboard;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = PartChrome, Type = typeof(VaultCipherFieldChrome))]
[TemplatePart(Name = PartRevealMenuItem, Type = typeof(MenuFlyoutItem))]
[TemplatePart(Name = PartConcealMenuItem, Type = typeof(MenuFlyoutItem))]
[TemplateVisualState(Name = StateConcealed, GroupName = GroupRevealStates)]
[TemplateVisualState(Name = StateRevealed, GroupName = GroupRevealStates)]
[DependencyProperty<string>("Label", DefaultValue = "")]
[DependencyProperty<string>("Password")]
[DependencyProperty<string>("DisplayText", DefaultValue = "")]
public sealed partial class VaultCipherPasswordField : Control
{
    private const string PartChrome = "PART_Chrome";
    private const string PartRevealMenuItem = "PART_RevealMenuItem";
    private const string PartConcealMenuItem = "PART_ConcealMenuItem";
    private const string GroupRevealStates = "RevealStates";
    private const string StateConcealed = "Concealed";
    private const string StateRevealed = "Revealed";
    private static readonly string MaskPasswordText = new('\u2022', 10);

    private bool _isRevealed;
    private VaultCipherFieldChrome? _chrome;
    private MenuFlyoutItem? _revealMenuItem;
    private MenuFlyoutItem? _concealMenuItem;

    public VaultCipherPasswordField()
    {
        DefaultStyleKey = typeof(VaultCipherPasswordField);
    }

    partial void OnPasswordChanged() => UpdateDisplayText();

    protected override void OnApplyTemplate()
    {
        _chrome?.Click -= OnChromeClick;
        _revealMenuItem?.Click -= OnRevealMenuItemClick;
        _concealMenuItem?.Click -= OnConcealMenuItemClick;

        base.OnApplyTemplate();

        _chrome = GetTemplateChild(PartChrome) as VaultCipherFieldChrome;
        _revealMenuItem = GetTemplateChild(PartRevealMenuItem) as MenuFlyoutItem;
        _concealMenuItem = GetTemplateChild(PartConcealMenuItem) as MenuFlyoutItem;

        _chrome?.Click += OnChromeClick;
        _revealMenuItem?.Click += OnRevealMenuItemClick;
        _concealMenuItem?.Click += OnConcealMenuItemClick;

        UpdateRevealState(useTransitions: false);
    }

    private void OnChromeClick(SplitButton sender, SplitButtonClickEventArgs args) =>
        ClipboardManager.SetText(Password);

    private void OnRevealMenuItemClick(object sender, RoutedEventArgs e)
    {
        _isRevealed = true;
        UpdateRevealState();
    }

    private void OnConcealMenuItemClick(object sender, RoutedEventArgs e)
    {
        _isRevealed = false;
        UpdateRevealState();
    }

    private void UpdateRevealState(bool useTransitions = true)
    {
        UpdateDisplayText();

        VisualStateManager.GoToState(
            this,
            _isRevealed ? StateRevealed : StateConcealed,
            useTransitions);
    }

    private void UpdateDisplayText()
    {
        DisplayText = _isRevealed
            ? Password ?? string.Empty
            : string.IsNullOrEmpty(Password)
                ? string.Empty
                : MaskPasswordText;
    }
}
