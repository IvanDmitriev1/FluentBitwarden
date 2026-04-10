using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FluentBitwarden.Resources.UserControls;

[DependencyProperty<Cipher>("Cipher")]
[DependencyProperty<double>("IconSize", DefaultValue = 60)]
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
}