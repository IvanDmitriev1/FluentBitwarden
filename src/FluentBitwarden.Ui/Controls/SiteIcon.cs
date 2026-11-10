using FluentBitwarden.Platform.SiteIcons;
using FluentBitwarden.Ui.Infrastructure;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentBitwarden.Ui.Controls;

[TemplatePart(Name = PartImageName, Type = typeof(Image))]
[TemplatePart(Name = PartFallbackIconName, Type = typeof(FontIcon))]
[DependencyProperty<Uri>("Uri")]
[DependencyProperty<string>("FallbackGlyph", DefaultValue = "\uE774")]
[DependencyProperty<double>("Size", DefaultValue = 28)]
public sealed partial class SiteIcon : ContentControl
{
    private const string PartImageName = "PART_Image";
    private const string PartFallbackIconName = "PART_FallbackIcon";

    public SiteIcon()
    {
        DefaultStyleKey = typeof(SiteIcon);

        _cache = App.Current.GetRequiredService<ISiteIconCache>();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _foregroundCallbackRegistration =
            new DependencyPropertyCallbackRegistration(
                this,
                ForegroundProperty,
                static (sender, dp) => ((SiteIcon)sender).Refresh());
    }

    private readonly ISiteIconCache _cache;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DependencyPropertyCallbackRegistration _foregroundCallbackRegistration;

    private string? _host;
    private Image? _image;
    private FontIcon? _fallback;
    private bool _isSubscribedToIconCached;

    partial void OnUriChanged() => Refresh();
    partial void OnFallbackGlyphChanged() => Refresh();
    partial void OnSizeChanged() => Refresh();

    protected override void OnApplyTemplate()
    {
        Watch(false);

        if (_image is not null)
        {
            _image.ImageOpened -= OnImageOpened;
            _image.ImageFailed -= OnImageFailed;
        }

        _foregroundCallbackRegistration.Unregister();

        base.OnApplyTemplate();

        _image = GetTemplateChild(PartImageName) as Image;
        _fallback = GetTemplateChild(PartFallbackIconName) as FontIcon;

        if (_image is not null)
        {
            _image.ImageOpened += OnImageOpened;
            _image.ImageFailed += OnImageFailed;
        }

        _foregroundCallbackRegistration.Register();
        Refresh();
    }


    private void Refresh()
    {
        if (!IsLoaded || _image is null || _fallback is null)
            return;

        Volatile.Write(ref _host, Uri?.Host);

        if (Uri is null || !double.IsFinite(Size) || Size <= 0)
        {
            Watch(false);
            SetImage(null);
            return;
        }

        Uri? path = _cache.TryGetCachedFilePath(Uri);
        SetImage(path);

        if (path is null)
        {
            Watch(true);
        }
    }

    private void Watch(bool enabled)
    {
        if (_isSubscribedToIconCached == enabled)
            return;

        _isSubscribedToIconCached = enabled;

        if (enabled)
            _cache.IconCached += OnIconCached;
        else
            _cache.IconCached -= OnIconCached;
    }

    private void SetImage(Uri? path)
    {
        int size = double.IsFinite(Size) && Size > 0
            ? (int)Math.Clamp(Math.Ceiling(Size), 1, int.MaxValue)
            : 1;

        _fallback!.Visibility = Visibility.Visible;
        _image!.Source = null;

        if (path is null)
            return;

        var bitmap = new BitmapImage
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = size,
            DecodePixelHeight = size,
            UriSource = path
        };

        _image.Source = bitmap;
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        _fallback!.Visibility = Visibility.Collapsed;
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _fallback!.Visibility = Visibility.Visible;
    }

    private void OnIconCached(object? sender, SiteIconCachedEventArgs e)
    {
        if (!string.Equals(
                Volatile.Read(ref _host),
                e.Host.Host,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Refresh);
    }
}
