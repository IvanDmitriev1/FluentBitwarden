namespace FluentBitwarden.Platform.SiteIcons;

public sealed class SiteIconCachedEventArgs(Uri host, string filePath) : EventArgs
{
    public Uri Host { get; } = host;
    public string FilePath { get; } = filePath;
}
