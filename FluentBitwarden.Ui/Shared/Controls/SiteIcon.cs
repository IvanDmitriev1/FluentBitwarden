using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.Views.Vault.Browse.Controls;

[DependencyProperty<Uri>("Uri")]
[DependencyProperty<string>("FallbackGlyph", DefaultValue = "\uE774")]
[DependencyProperty<double>("Size", DefaultValue = DefaultSize)]
public sealed partial class SiteIcon : ContentControl
{
    private enum SiteIconContentKind { None, Fallback, Image }

    private const double DefaultSize = 20;

    [field: AllowNull, MaybeNull]
    private ISiteIconCache SiteIconCache =>
        field ??= App.Current.GetRequiredService<ISiteIconCache>();

    private SiteIconContentKind _contentKind;
    private Uri? _displayedSiteUri;
    private Uri? _displayedCachedFilePath;
    private int _displayedDecodeSize;
    private bool _isSubscribed;
    private readonly DispatcherQueue _dispatcherQueue;

    public SiteIcon()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DefaultStyleKey = typeof(SiteIcon);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UpdateCurrentContent();
        Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isSubscribed)
        {
            SiteIconCache.IconCached += SiteIconCacheOnIconCached;
            _isSubscribed = true;
        }

        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isSubscribed)
        {
            SiteIconCache.IconCached -= SiteIconCacheOnIconCached;
            _isSubscribed = false;
        }
    }

    partial void OnSizeChanged(double newValue)
    {
        if (double.IsNaN(newValue) || double.IsInfinity(newValue) || newValue <= 0)
        {
            Size = DefaultSize;
            return;
        }

        UpdateCurrentContent();
        Refresh();
    }

    partial void OnUriChanged()
    {
        InvalidateDisplayedIcon();
        Refresh();
    }

    partial void OnFallbackGlyphChanged()
    {
        UpdateCurrentContent();
    }

    private void SiteIconCacheOnIconCached(object? sender, SiteIconCachedEventArgs e)
    {
        var siteUri = e.Host;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => RefreshIfCurrent(siteUri));
    }

    private void RefreshIfCurrent(Uri siteUri)
    {
        if (!IsLoaded || siteUri != Uri)
            return;

        Refresh();
    }

    private void Refresh()
    {
        if (!IsLoaded)
            return;

        if (Uri is null || SiteIconCache.TryGetCachedFilePath(Uri) is not { } cachedFilePath)
        {
            ShowFallback();
            return;
        }

        int decodeSize = Math.Max(1, (int)Math.Ceiling(Size));

        if (_contentKind == SiteIconContentKind.Image
            && Uri == _displayedSiteUri
            && cachedFilePath == _displayedCachedFilePath
            && decodeSize == _displayedDecodeSize)
        {
            return;
        }

        ShowImage(CreateImageSource(cachedFilePath, decodeSize), cachedFilePath, decodeSize);
    }

    private void ShowImage(ImageSource source, Uri cachedFilePath, int decodeSize)
    {
        if (_contentKind == SiteIconContentKind.Image && Content is Border { Child: Image image } frame)
        {
            UpdateImageFrame(frame);
            image.Source = source;
        }
        else
        {
            Content = CreateImageContent(source);
            _contentKind = SiteIconContentKind.Image;
        }

        _displayedSiteUri = Uri;
        _displayedCachedFilePath = cachedFilePath;
        _displayedDecodeSize = decodeSize;
    }

    private void ShowFallback()
    {
        if (_contentKind == SiteIconContentKind.Fallback && Content is FontIcon fallbackIcon)
        {
            UpdateFallbackContent(fallbackIcon);
        }
        else
        {
            Content = CreateFallbackContent();
            _contentKind = SiteIconContentKind.Fallback;
        }

        _displayedSiteUri = Uri;
        _displayedCachedFilePath = null;
        _displayedDecodeSize = 0;
    }

    private void UpdateCurrentContent()
    {
        switch (_contentKind)
        {
            case SiteIconContentKind.Fallback when Content is FontIcon fallbackIcon:
                UpdateFallbackContent(fallbackIcon);
                break;
            case SiteIconContentKind.Image when Content is Border { Child: Image } frame:
                UpdateImageFrame(frame);
                break;
        }
    }

    private void InvalidateDisplayedIcon()
    {
        _displayedSiteUri = null;
        _displayedCachedFilePath = null;
        _displayedDecodeSize = 0;
    }

    private FontIcon CreateFallbackContent()
    {
        var icon = new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        UpdateFallbackContent(icon);
        return icon;
    }

    private void UpdateFallbackContent(FontIcon icon)
    {
        icon.Width = Size;
        icon.Height = Size;
        icon.Glyph = FallbackGlyph;
        icon.FontSize = Size;
        icon.Foreground = Foreground;
    }

    private Border CreateImageContent(ImageSource source)
    {
        var frame = new Border
        {
            Child = new Image
            {
                Source = source,
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false
            }
        };

        UpdateImageFrame(frame);
        return frame;
    }

    private void UpdateImageFrame(Border frame)
    {
        frame.Width = Size;
        frame.Height = Size;
        frame.CornerRadius = new CornerRadius(Math.Clamp(Size * 0.18, 4, 8));
    }

    private static ImageSource CreateImageSource(Uri cachedFilePath, int decodeSize)
    {
        return new BitmapImage
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = decodeSize,
            DecodePixelHeight = decodeSize,
            UriSource = cachedFilePath
        };
    }
}
