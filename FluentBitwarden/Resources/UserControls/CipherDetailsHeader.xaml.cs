using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using FluentBitwarden.Shared.Converters;

namespace FluentBitwarden.Resources.UserControls;

[DependencyProperty<Cipher>("Cipher")]
[DependencyProperty<double>("IconSize", DefaultValue = 36)]
[DependencyProperty<double>("IconCornerRadius", DefaultValue = 10)]
public sealed partial class CipherDetailsHeader : UserControl
{
    public CipherDetailsHeader()
    {
        InitializeComponent();
    }

    partial void OnCipherChanged()
    {
        if (Cipher is null)
            return;

        Title.Text = Cipher.Name;

        Uri? iconUri = null;
        string iconGlyph = Cipher.GetGlyph();

        if (Cipher is LoginCipher loginCipher)
        {
            var url = loginCipher.Uris.FirstOrDefault();
            StringToUriConverter.TryConvert(url, out iconUri);
        }

        IconHost.FallbackGlyph = iconGlyph;
        IconHost.Uri = iconUri;
        IconHost.Size = IconSize;
    }
}