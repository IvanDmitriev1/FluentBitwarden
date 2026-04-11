namespace FluentBitwarden.Shared.SiteIcons;

internal interface ISiteIconCache
{
    ValueTask<string?> GetOrFetchAsync(Uri iconUri, CancellationToken cancellationToken = default);
}
