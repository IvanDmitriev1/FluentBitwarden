using BitwardenApi.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Resources.Converters;

namespace FluentBitwarden.Resources.UserControls;

[DependencyProperty<VaultCipher>("VaultCipher")]
[DependencyProperty<double>("IconSize", DefaultValue = 36)]
[DependencyProperty<double>("IconCornerRadius", DefaultValue = 10)]
public sealed partial class CipherDetailsHeader : UserControl
{
    public CipherDetailsHeader()
    {
        InitializeComponent();
    }

    partial void OnVaultCipherChanged()
    {
        if (VaultCipher is null)
            return;

        Title.Text = VaultCipher.Name;

        Uri? iconUri = null;
        string iconGlyph = VaultCipher.GetGlyph();

        if (VaultCipher is LoginVaultCipher loginCipher)
        {
            var url = loginCipher.Uris.FirstOrDefault();
            StringToUriConverter.TryConvert(url, out iconUri);
        }

        IconHost.FallbackGlyph = iconGlyph;
        IconHost.Uri = iconUri;
        IconHost.Size = IconSize;
    }
}