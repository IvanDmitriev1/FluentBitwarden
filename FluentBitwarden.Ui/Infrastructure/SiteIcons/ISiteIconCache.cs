namespace FluentBitwarden.Infrastructure.SiteIcons;

public sealed class SiteIconCachedEventArgs(Uri host, string filePath) : EventArgs
{
    public Uri Host { get; } = host;
    public string FilePath { get; } = filePath;
}

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
    Task PreloadAsync(IEnumerable<Uri> siteUris);
}
