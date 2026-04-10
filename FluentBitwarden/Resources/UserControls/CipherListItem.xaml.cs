using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FluentBitwarden.Resources.UserControls;

[DependencyProperty<Cipher>("Cipher")]
[DependencyProperty<double>("IconSize", DefaultValue = 40)]
[DependencyProperty<double>("IconCornerRadius", DefaultValue = 10)]
public sealed partial class CipherListItem : UserControl
{
    public CipherListItem()
    {
        InitializeComponent();
    }

    partial void OnCipherChanged()
    {
        if (Cipher is null)
            return;

        TitleTextBlock.Text = Cipher.Name;
        SubtitleTextBlock.Text = BuildSubtitle(Cipher);
        IconHost.Content = CreateFallbackIcon(Cipher);
    }

    private UIElement CreateFallbackIcon(Cipher cipher) => new FontIcon
    {
        Glyph = cipher.GetGlyph(),
        FontFamily = new FontFamily("Segoe Fluent Icons"),
        FontSize = IconSize * 0.55,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

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