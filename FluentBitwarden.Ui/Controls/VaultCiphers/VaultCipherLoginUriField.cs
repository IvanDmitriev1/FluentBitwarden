using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Exception = System.Exception;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = TextBlockPartName, Type = typeof(TextBlock))]
[DependencyProperty<LoginUri>("Uri")]
[DependencyProperty<Brush>("DomainForeground")]
[DependencyProperty<Brush>("SecondaryForeground")]
public sealed partial class VaultCipherLoginUriField : VaultCipherFieldControlBase
{
    private readonly record struct UriParts(string Prefix, string Domain, string Suffix);

    private const string TextBlockPartName = "PART_TextBlock";

    private TextBlock? _textBlock;

    public VaultCipherLoginUriField()
    {
        DefaultStyleKey = typeof(VaultCipherLoginUriField);
    }

    protected override FlyoutBase? CreateMenuFlyout()
    {
        return null;
    }

    protected override async void OnPrimaryAction()
    {
        if (Uri is null || !Uri.TryGetWebUri(out var launchUri))
            return;

        try
        {
            await Launcher.LaunchUriAsync(launchUri);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to launch URI '{launchUri}': {exception}");
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBlock = GetTemplateChild(TextBlockPartName) as TextBlock;
        UpdateText();
    }

    partial void OnUriChanged() => UpdateText();
    partial void OnDomainForegroundChanged() => UpdateText();
    partial void OnSecondaryForegroundChanged() => UpdateText();

    private void UpdateText()
    {
        if (_textBlock is null)
            return;

        _textBlock.Inlines.Clear();

        if (Uri is not { } loginUri || string.IsNullOrWhiteSpace(loginUri.Value))
        {
            return;
        }

        if (!TryCreateUriParts(loginUri, out var parts))
        {
            AddRun(loginUri.Value, SecondaryForeground);
            return;
        }

        AddRun(parts.Prefix, SecondaryForeground);
        AddRun(parts.Domain, DomainForeground);
        AddRun(parts.Suffix, SecondaryForeground);
    }

    private void AddRun(string text, Brush? foreground)
    {
        if (string.IsNullOrEmpty(text))
            return;

        _textBlock?.Inlines.Add(new Run
        {
            Text = text,
            Foreground = foreground
        });
    }

    private static bool TryCreateUriParts(LoginUri loginUri, out UriParts parts)
    {
        parts = default;

        if (loginUri.TryGetAbsoluteUri(out var absoluteUri) && !loginUri.IsWebUri)
        {
            if (string.IsNullOrWhiteSpace(absoluteUri.Host))
                return false;

            parts = new UriParts(string.Empty, absoluteUri.Host, string.Empty);
            return true;
        }

        if (!loginUri.TryGetWebUri(out var webUri) || string.IsNullOrWhiteSpace(webUri.Host))
            return false;

        var hostStart = loginUri.Value.IndexOf(webUri.Host, StringComparison.OrdinalIgnoreCase);
        if (hostStart < 0)
            return false;

        parts = new UriParts(
            loginUri.Value[..hostStart],
            loginUri.Value.Substring(hostStart, webUri.Host.Length),
            loginUri.Value[(hostStart + webUri.Host.Length)..]);

        return true;
    }
}
