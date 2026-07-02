using FluentBitwarden.Platform.Infrastructure.Clipboard;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = PartChrome, Type = typeof(VaultCipherFieldChrome))]
[DependencyProperty<string>("Label", DefaultValue = "")]
[DependencyProperty<string>("Text")]
[DependencyProperty<string>("ActionText")]
public partial class VaultCipherTextField : Control
{
    private const string PartChrome = "PART_Chrome";

    private VaultCipherFieldChrome? _chrome;

    public VaultCipherTextField()
    {
        DefaultStyleKey = typeof(VaultCipherTextField);
    }

    protected override void OnApplyTemplate()
    {
        _chrome?.Click -= OnChromeClick;

        base.OnApplyTemplate();

        _chrome = GetTemplateChild(PartChrome) as VaultCipherFieldChrome;

        _chrome?.Click += OnChromeClick;
    }

    private void OnChromeClick(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (string.IsNullOrEmpty(ActionText))
            return;
        
        ClipboardManager.SetText(Text);
    }
}
