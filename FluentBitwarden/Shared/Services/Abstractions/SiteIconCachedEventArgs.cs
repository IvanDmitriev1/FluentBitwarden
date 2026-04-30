namespace FluentBitwarden.Shared.Services.Abstractions;

public sealed class SiteIconCachedEventArgs(Uri host, string filePath) : EventArgs
{
    public Uri Host { get; } = host;
    public string FilePath { get; } = filePath;
}