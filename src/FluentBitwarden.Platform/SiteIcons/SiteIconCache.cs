using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace FluentBitwarden.Platform.SiteIcons;

internal sealed class SiteIconCache(IHttpClientFactory httpClientFactory, ILogger<SiteIconCache> logger)
    : ISiteIconCache
{
    private static readonly string CacheDirectoryPath =
        Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "SiteIcons");

    private readonly ConcurrentDictionary<Uri, Uri> _cachedFilePaths = [];

    public event EventHandler<SiteIconCachedEventArgs>? IconCached;

    public Uri? TryGetCachedFilePath(Uri siteUri)
    {
        if (_cachedFilePaths.TryGetValue(siteUri, out var cachedFilePath))
            return cachedFilePath;

        string fileName = GetFileName(siteUri);
        string filePath = Path.Combine(CacheDirectoryPath, fileName);

        if (!File.Exists(filePath))
            return null;

        cachedFilePath = new Uri(filePath, UriKind.Absolute);
        _cachedFilePaths.TryAdd(siteUri, cachedFilePath);
        return cachedFilePath;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; preload is best-effort and must not fail the caller.")]
    public async Task PreloadAsync(IEnumerable<Uri> siteUris)
    {
        Directory.CreateDirectory(CacheDirectoryPath);
        CancellationTokenSource cts = new(TimeSpan.FromSeconds(8));

        var options = new ParallelOptions()
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = 4
        };

        try
        {
            await Parallel.ForEachAsync(siteUris, options, CacheIconAsync);
        }
        catch (OperationCanceledException e)
        {
            logger.PreloadCanceled(e);
        }
        catch (Exception e)
        {
            logger.PreloadFailed(e);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; a single icon fetch failure must not fail the whole preload batch.")]
    private async ValueTask CacheIconAsync(Uri siteUri, CancellationToken cancellationToken)
    {
        try
        {
            string fileName = GetFileName(siteUri);
            string filePath = Path.Combine(CacheDirectoryPath, fileName);

            if (File.Exists(filePath))
            {
                _cachedFilePaths.TryAdd(siteUri, new Uri(filePath, UriKind.Absolute));
                return;
            }

            using var httpClient = httpClientFactory.CreateSiteIconClient();

            using var response = await httpClient.GetAsync(
                new Uri($"https://icons.bitwarden.net/{siteUri.Host}/icon.png"),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return;

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            var tmpPath = Path.GetTempFileName();

            await using (var tmpFileStream = new FileStream(tmpPath, FileMode.Create,
                             FileAccess.Write, FileShare.None, 1024 * 32,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(tmpFileStream, cancellationToken);
            }

            File.Move(tmpPath, filePath, true);
            _cachedFilePaths[siteUri] = new Uri(filePath, UriKind.Absolute);
            IconCached?.Invoke(this, new SiteIconCachedEventArgs(siteUri, filePath));
        }
        catch (OperationCanceledException e)
        {
            Debug.WriteLine($"Site icon cache preload was canceled for {siteUri}: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Site icon cache preload failed for {siteUri}: {e}");
        }
    }

    private static string GetFileName(Uri uri)
    {
        const string extension = ".png";
        const int hashLength = 32;

        return string.Create(hashLength * 2 + extension.Length, uri, static (destination, uri) =>
        {
            ReadOnlySpan<char> text = uri.Host.AsSpan();

            int length = Encoding.UTF8.GetByteCount(text);
            bool useStackAlloc = length <= 512;

            Span<byte> buffer = useStackAlloc
                ? stackalloc byte[length]
                : new byte[length];

            int written = Encoding.UTF8.GetBytes(text, buffer);

            Span<byte> hash = stackalloc byte[hashLength];
            if (!SHA256.TryHashData(buffer[..written], hash, out int hashWritten))
                throw new InvalidOperationException("SHA256 hashing failed.");

            if (!Convert.TryToHexString(hash, destination, out int charsWritten))
                throw new InvalidOperationException("Convert.TryToHexString failed.");

            extension.AsSpan().CopyTo(destination[charsWritten..]);
        });
    }
}