using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Shared.Extensions;
using Microsoft.UI.Xaml;
using System.Linq;

namespace FluentBitwarden.Resources.Controls;

[TemplatePart(Name = PartIconHost, Type = typeof(SiteIcon))]
[TemplatePart(Name = PartTitleTextBlock, Type = typeof(TextBlock))]
[TemplatePart(Name = PartSubtitleTextBlock, Type = typeof(TextBlock))]
[DependencyProperty<Cipher>("Cipher")]
[DependencyProperty<double>("IconSize", DefaultValue = 22)]
[DependencyProperty<double>("IconCornerRadius", DefaultValue = 10)]
public sealed partial class CipherListItem : Control
{
    private const string PartIconHost = "PART_IconHost";
    private const string PartTitleTextBlock = "PART_TitleTextBlock";
    private const string PartSubtitleTextBlock = "PART_SubtitleTextBlock";
    private const string DefaultFallbackGlyph = "\uE774";

    private SiteIcon? _iconHost;
    private TextBlock? _titleTextBlock;
    private TextBlock? _subtitleTextBlock;

    public CipherListItem()
    {
        DefaultStyleKey = typeof(CipherListItem);
    }

    partial void OnCipherChanged()
        => Refresh();

    partial void OnIconSizeChanged()
        => RefreshIcon();

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconHost = GetTemplateChild(PartIconHost) as SiteIcon;
        _titleTextBlock = GetTemplateChild(PartTitleTextBlock) as TextBlock;
        _subtitleTextBlock = GetTemplateChild(PartSubtitleTextBlock) as TextBlock;

        Refresh();
    }

    private void Refresh()
    {
        if (Cipher is null)
        {
            Clear();
            return;
        }

        if (_titleTextBlock is not null)
        {
            _titleTextBlock.Text = Cipher.Name;
        }

        if (_subtitleTextBlock is not null)
        {
            _subtitleTextBlock.Text = BuildSubtitle(Cipher);
        }

        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (_iconHost is null)
        {
            return;
        }

        if (Cipher is null)
        {
            _iconHost.FallbackGlyph = DefaultFallbackGlyph;
            _iconHost.Uri = null;
            _iconHost.Size = IconSize;
            return;
        }

        Uri? iconUri = null;

        if (Cipher is LoginCipher loginCipher)
        {
            string? url = loginCipher.Uris.FirstOrDefault();
            _ = Uri.TryCreate(url, UriKind.Absolute, out iconUri);
        }

        _iconHost.FallbackGlyph = Cipher.GetGlyph();
        _iconHost.Uri = iconUri;
        _iconHost.Size = IconSize;
    }

    private void Clear()
    {
        if (_titleTextBlock is not null)
        {
            _titleTextBlock.Text = string.Empty;
        }

        if (_subtitleTextBlock is not null)
        {
            _subtitleTextBlock.Text = string.Empty;
        }

        RefreshIcon();
    }

    private static string? BuildSubtitle(Cipher cipher) => cipher switch
    {
        CardCipher cardCipher => cardCipher.Brand,
        IdentityCipher identityCipher => identityCipher.Title,
        LoginCipher loginCipher => loginCipher.Username,
        SecureNoteCipher _ => null,
        SshKeyCipher sshKeyCipher => sshKeyCipher.KeyFingerprint,
        _ => throw new ArgumentOutOfRangeException(nameof(cipher))
    };
}
