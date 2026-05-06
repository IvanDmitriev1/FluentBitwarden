namespace FluentBitwarden.Infrastructure.Services.Abstractions;

public interface ISiteIconCache
{
    /// <summary>
    /// Raised after an icon has been downloaded and written to disk.
    /// The event is raised from a background thread.
    /// </summary>
    event EventHandler<SiteIconCachedEventArgs>? IconCached;

    Uri? TryGetCachedFilePath(Uri siteUri);

    /// <summary>
    /// Downloads missing icons for the provided site URIs. Intended to run after vault unlock.
    /// </summary>
    Task PreloadAsync(IEnumerable<Uri> siteUris, CancellationToken cancellationToken = default);
}
