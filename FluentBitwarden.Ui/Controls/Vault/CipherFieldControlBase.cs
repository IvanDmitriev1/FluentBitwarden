using System.Windows.Input;
using FluentBitwarden.Infrastructure.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.Controls.Vault;

[TemplatePart(Name = PartChrome, Type = typeof(CipherFieldChrome))]
[DependencyProperty<string>("Label", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<ICommand>("Command", DefaultBindingMode = DefaultBindingMode.OneTime)]
public abstract partial class CipherFieldControlBase : Control
{
    private const string PartChrome = "PART_Chrome";

    private CipherFieldChrome? _chrome;

    protected abstract FlyoutBase? CreateMenuFlyout();
    protected abstract void OnPrimaryAction();

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_chrome is not null)
        {
            _chrome.Click -= OnChromeClick;
        }

        _chrome = GetTemplateChild("PART_Chrome") as CipherFieldChrome;

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

        _chrome.MenuFlyout = CreateMenuFlyout();
    }

    private void OnChromeClick(object sender, RoutedEventArgs e)
    {
        OnPrimaryAction();

        if (Command is not null && Command.CanExecute(null))
        {
            Command.Execute(null);
        }
    }
}
