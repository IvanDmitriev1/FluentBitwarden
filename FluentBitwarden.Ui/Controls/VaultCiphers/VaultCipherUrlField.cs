using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = TextBlockPartName, Type = typeof(TextBox))]
[DependencyProperty<string>("Url")]
public sealed partial class VaultCipherUrlField : VaultCipherFieldControlBase
{
    private readonly record struct UriParts(string Prefix, string Host, string Suffix);

    private const string TextBlockPartName = "PART_TextBlock";

    private TextBlock? _textBlock;

    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override void OnPrimaryAction()
    {
        
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBlock = GetTemplateChild(TextBlockPartName) as TextBlock;
    }

    partial void OnUrlChanged(string? newValue) => UpdateText();

    private void UpdateText()
    {
        if (_textBlock is null || string.IsNullOrWhiteSpace(Url))
        {
            _textBlock?.Inlines.Clear();
            return;
        }


    }
}
