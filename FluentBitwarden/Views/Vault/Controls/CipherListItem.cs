using BitwardenApi.Models;
using FluentBitwarden.UI.Controls;
using FluentBitwarden.Resources.Converters;
using Microsoft.UI.Xaml;
using System.Linq;
using FluentBitwarden.Infrastructure.Extensions;

namespace FluentBitwarden.Views.Vault.Controls;

[TemplatePart(Name = PartIconHost, Type = typeof(SiteIcon))]
[TemplatePart(Name = PartTitleTextBlock, Type = typeof(TextBlock))]
[TemplatePart(Name = PartSubtitleTextBlock, Type = typeof(TextBlock))]
[DependencyProperty<VaultCipher>("VaultCipher")]
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

    partial void OnVaultCipherChanged()
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
        if (VaultCipher is null)
        {
            Clear();
            return;
        }

        _titleTextBlock?.Text = VaultCipher.Name;
        _subtitleTextBlock?.Text = BuildSubtitle(VaultCipher);
        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (_iconHost is null)
        {
            return;
        }

        if (VaultCipher is null)
        {
            _iconHost.FallbackGlyph = DefaultFallbackGlyph;
            _iconHost.Uri = null;
            _iconHost.Size = IconSize;
            return;
        }

        Uri? iconUri = null;

        if (VaultCipher is LoginVaultCipher loginCipher)
        {
            string? url = loginCipher.Uris.FirstOrDefault();
            StringToUriConverter.TryConvert(url, out iconUri);
        }

        _iconHost.FallbackGlyph = VaultCipher.GetGlyph();
        _iconHost.Uri = iconUri;
        _iconHost.Size = IconSize;
    }

    private void Clear()
    {
        _titleTextBlock?.Text = string.Empty;
        _subtitleTextBlock?.Text = string.Empty;
        RefreshIcon();
    }

    private static string? BuildSubtitle(VaultCipher vaultCipher) => vaultCipher switch
    {
        CardVaultCipher cardCipher => cardCipher.Brand,
        IdentityVaultCipher identityCipher => identityCipher.Title,
        LoginVaultCipher loginCipher => loginCipher.Username,
        SecureNoteVaultCipher _ => null,
        SshKeyVaultCipher sshKeyCipher => sshKeyCipher.KeyFingerprint,
        _ => throw new ArgumentOutOfRangeException(nameof(vaultCipher))
    };
}
