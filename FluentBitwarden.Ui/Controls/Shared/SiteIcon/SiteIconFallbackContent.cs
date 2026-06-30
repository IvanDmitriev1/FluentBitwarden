using Microsoft.UI.Xaml.Media;

namespace FluentBitwarden.Controls.Shared.SiteIcon;

public sealed record SiteIconFallbackContent(
    string Glyph,
    double Size,
    Brush Foreground)
{
    public bool IsSame(string glyph, Brush foreground) => Glyph == glyph && ReferenceEquals(Foreground, foreground);
}
