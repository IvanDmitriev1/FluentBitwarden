using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Shared.Extensions;
using System.Linq;

namespace FluentBitwarden.Resources.UserControls;

[DependencyProperty<Cipher>("Cipher")]
[DependencyProperty<double>("IconSize", DefaultValue = 22)]
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


        Uri? iconUri = null;
        string iconGlyph = Cipher.GetGlyph();

        if (Cipher is LoginCipher loginCipher)
        {
            var url = loginCipher.Uris.FirstOrDefault();
            bool result = Uri.TryCreate(url, UriKind.Absolute, out iconUri);
        }

        IconHost.FallbackGlyph = iconGlyph;
        IconHost.Uri = iconUri;
        IconHost.Size = IconSize;
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