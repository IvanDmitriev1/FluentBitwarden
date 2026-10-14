using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentBitwarden.Controls.Shared.SiteIcon;

public sealed record SiteIconImageContent(
    ImageSource Source,
    double Size,
    CornerRadius CornerRadius)
{
    public bool IsSame(Uri cachedFilePath, int size) => Source is BitmapImage bitmapImage &&
                                              bitmapImage.DecodePixelWidth == size &&
                                              bitmapImage.DecodePixelHeight == size &&
                                              bitmapImage.UriSource == cachedFilePath;
}