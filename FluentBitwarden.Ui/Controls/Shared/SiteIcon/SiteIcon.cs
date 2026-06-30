using FluentBitwarden.Platform.SiteIcons;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Controls.Shared.SiteIcon;

[DependencyProperty<Uri>("Uri")]
[DependencyProperty<string>("FallbackGlyph", DefaultValue = "\uE774")]
[DependencyProperty<double>("Size", DefaultValue = 28)]
public sealed partial class SiteIcon : ContentControl
{
    [field: AllowNull, MaybeNull]
    private ISiteIconCache SiteIconCache =>
        field ??= App.Current.GetRequiredService<ISiteIconCache>();

    private int NormalizedSize => Math.Max(1, (int)Math.Ceiling(Size));

    private readonly DispatcherQueue _dispatcherQueue;
    private bool _isSubscribedToIconCached;

    public SiteIcon()
    {
        DefaultStyleKey = typeof(SiteIcon);
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        RegisterPropertyChangedCallback(ForegroundProperty, static (sender, dp) => ((SiteIcon)sender).Refresh());
    }

    partial void OnUriChanged() => Refresh();
    partial void OnFallbackGlyphChanged() => Refresh();
    partial void OnSizeChanged() => Refresh();

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UnsubscribeFromIconCached();
    }

    private void SiteIconCacheOnIconCached(object? sender, SiteIconCachedEventArgs e)
    {
        var cachedSiteUri = e.Host;

        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (!IsLoaded || !Equals(Uri, cachedSiteUri))
                return;

            Refresh();
        });
    }

    private void Refresh()
    {
        if (!IsLoaded || !IsValidSize(Size))
            return;

        if (Uri is not { } siteUri)
        {
            ShowFallback(waitForCachedIcon: false);
            return;
        }

        if (SiteIconCache.TryGetCachedFilePath(siteUri) is not { } cachedFilePath)
        {
            ShowFallback(waitForCachedIcon: true);
            return;
        }

        ShowImage(cachedFilePath);
    }

    private void SetTemplatedContent(object content)
    {
        Content = content;
        ContentTemplate = ContentTemplateSelector?.SelectTemplate(content, this);
    }

    private void ShowFallback(bool waitForCachedIcon)
    {
        if (Content is not SiteIconFallbackContent currentContent || 
            !currentContent.IsSame(FallbackGlyph, Foreground))
        {
            SetTemplatedContent(new SiteIconFallbackContent(FallbackGlyph, Size, Foreground));
        }

        if (waitForCachedIcon)
            SubscribeToIconCached();
        else
            UnsubscribeFromIconCached();
    }

    private void ShowImage(Uri cachedFilePath)
    {
        int size = NormalizedSize;

        if (Content is SiteIconImageContent imageContent && imageContent.IsSame(cachedFilePath, size))
        {
            UnsubscribeFromIconCached();
            return;
        }

        var imageSource = new BitmapImage
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = size,
            DecodePixelHeight = size,
            UriSource = cachedFilePath
        };
        var cornerRadius = new CornerRadius(Math.Clamp(Size * 0.18, 4, 8));

        SetTemplatedContent(new SiteIconImageContent(imageSource, size, cornerRadius));
        UnsubscribeFromIconCached();
    }

    private void SubscribeToIconCached()
    {
        if (_isSubscribedToIconCached)
            return;

        SiteIconCache.IconCached += SiteIconCacheOnIconCached;
        _isSubscribedToIconCached = true;
    }

    private void UnsubscribeFromIconCached()
    {
        if (!_isSubscribedToIconCached)
            return;

        SiteIconCache.IconCached -= SiteIconCacheOnIconCached;
        _isSubscribedToIconCached = false;
    }

    private static bool IsValidSize(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
}
