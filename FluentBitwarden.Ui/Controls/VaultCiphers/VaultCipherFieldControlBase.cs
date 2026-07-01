using System.Windows.Input;
using FluentBitwarden.Platform.Infrastructure.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = PartChrome, Type = typeof(VaultCipherFieldChrome))]
[DependencyProperty<ICommand>("Command", DefaultBindingMode = DefaultBindingMode.OneTime)]
public abstract partial class VaultCipherFieldControlBase : Control
{
    private const string PartChrome = "PART_Chrome";

    private VaultCipherFieldChrome? _chrome;

    protected abstract FlyoutBase? CreateMenuFlyout();
    protected abstract void OnPrimaryAction();

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_chrome is not null)
        {
            _chrome.Click -= OnChromeClick;
        }

        _chrome = GetTemplateChild("PART_Chrome") as VaultCipherFieldChrome;

        if (_chrome is not null)
        {
            _chrome.Click += OnChromeClick;
        }

        Refresh();
    }

    protected void CopyTextToClipboard(string? text)
    {
        ClipboardManager.SetText(text);
    }

    private void Refresh()
    {
        if (_chrome is null)
            return;

        _chrome.Flyout = CreateMenuFlyout();
    }

    private void OnChromeClick(SplitButton sender, SplitButtonClickEventArgs args)
    {
        OnPrimaryAction();

        if (Command is not null && Command.CanExecute(null))
        {
            Command.Execute(null);
        }
    }
}
