using FluentBitwarden.Shared.Services.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Resources.Controls;

[DependencyProperty<Uri>("Uri")]
[DependencyProperty<string>("FallbackGlyph", DefaultValue = "\uE774")]
[DependencyProperty<double>("Size", DefaultValue = 20)]
public sealed partial class SiteIcon : ContentControl
{
    [field: AllowNull, MaybeNull]
    private ISiteIconCache SiteIconCache => field ??= App.Current.GetRequiredService<ISiteIconCache>();

    private bool _isListeningForCacheUpdates;

    public SiteIcon()
    {
        DefaultStyleKey = typeof(SiteIcon);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        Content = CreateFallbackContent();
    }

    partial void OnSizeChanged(double newValue)
    {
        if (double.IsNaN(newValue) || double.IsInfinity(newValue) || newValue <= 0)
        {
            Size = 20;
            return;
        }
    }

    partial void OnUriChanged()
    {
        Content = CreateFallbackContent();
        Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Refresh();

        if (!_isListeningForCacheUpdates)
        {
            SiteIconCache.IconCached += SiteIconCacheOnIconCached;
            _isListeningForCacheUpdates = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;

        if (_isListeningForCacheUpdates)
        {
            SiteIconCache.IconCached -= SiteIconCacheOnIconCached;
            _isListeningForCacheUpdates = false;
        }
    }

    private void SiteIconCacheOnIconCached(object? sender, SiteIconCachedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (e.Host != Uri)
                return;

            Refresh();
        });
    }

    private void Refresh()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (Uri is null || SiteIconCache.TryGetCachedFilePath(Uri) is not { } cachedFilePath)
            return;

       
        int decodeSize = Math.Max(1, (int)Math.Ceiling(Size));
        var bitmap = new BitmapImage
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = decodeSize,
            DecodePixelHeight = decodeSize,
            UriSource = cachedFilePath
        };

        Content = CreateImageContent(bitmap);
    }

    private UIElement CreateFallbackContent()
        => new FontIcon
        {
            Width = Size,
            Height = Size,
            Glyph = FallbackGlyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = Size,
            Foreground = Foreground,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

    private UIElement CreateImageContent(ImageSource source)
        => new Border
        {
            Width = Size,
            Height = Size,
            CornerRadius = new CornerRadius(Math.Clamp(Size * 0.18, 4, 8)),
            Child = new Image
            {
                Source = source,
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false
            }
        };
}
