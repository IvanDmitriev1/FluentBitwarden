using System.Diagnostics;
using FluentBitwarden.Shared.SiteIcons;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentBitwarden.Resources.Controls;

[DependencyProperty<Uri>("Uri")]
[DependencyProperty<string>("FallbackGlyph", DefaultValue = "\uE774")]
[DependencyProperty<double>("Size", DefaultValue = 20)]
public sealed partial class SiteIcon : ContentControl
{
    private static readonly Dictionary<(string AbsolutePath, int DecodeSize), BitmapImage> ImageCache = [];

    private CancellationTokenSource? _cts;

    public SiteIcon()
    {
        DefaultStyleKey = typeof(SiteIcon);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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
        => _ = RefreshAsync();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _ = RefreshAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;

        CancelPendingLoad();
    }

    private async Task RefreshAsync()
    {
        CancelPendingLoad();
        Content = CreateFallbackContent();

        if (!IsLoaded)
        {
            return;
        }

        var requestedUri = Uri;
        if (requestedUri is null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        int decodeSize = Math.Max(1, (int)Math.Ceiling(Size));

        try
        {
            var iconCache = App.Current.GetRequiredService<ISiteIconCache>();
            string? absolutePath = await iconCache.GetOrFetchAsync(requestedUri, _cts.Token);
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return;
            }

            if (_cts.IsCancellationRequested)
            {
                return;
            }

            var cacheKey = (absolutePath, decodeSize);
            if (!ImageCache.TryGetValue(cacheKey, out var bitmap))
            {
                bitmap = CreateBitmapImage(absolutePath, decodeSize);
                ImageCache[cacheKey] = bitmap;
            }

            Content = CreateImageContent(bitmap);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            //
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
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

    private void CancelPendingLoad()
    {
        if (_cts is null)
        {
            return;
        }

        try
        {
            _cts.Cancel();
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    private static BitmapImage CreateBitmapImage(string absolutePath, int decodeSize)
    {
        var bitmap = new BitmapImage
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = decodeSize,
            DecodePixelHeight = decodeSize,
            UriSource = new Uri(absolutePath, UriKind.Absolute)
        };

        return bitmap;
    }
}
