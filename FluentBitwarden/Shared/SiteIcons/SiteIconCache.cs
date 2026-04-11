using CommunityToolkit.HighPerformance.Buffers;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;

namespace FluentBitwarden.Shared.SiteIcons;

[Fody.ConfigureAwait(false)]
internal sealed class SiteIconCache(HttpClient client) : ISiteIconCache
{
    private static readonly string CacheDirectoryPath =
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "SiteIcons");

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async ValueTask<string?> GetOrFetchAsync(Uri iconUri, CancellationToken cancellationToken = default)
    {
        var cacheEntry = await Task.Run(() => CreateCacheEntry(iconUri), cancellationToken);
        if (cacheEntry.Exists)
        {
            return cacheEntry.FilePath;
        }

        var gate = _locks.GetOrAdd(cacheEntry.FileName, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(cacheEntry.FilePath))
            {
                return cacheEntry.FilePath;
            }

            using var response = await client.GetAsync(
                $"https://icons.bitwarden.net/{iconUri.Host}/icon.png",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await DownloadImage(cacheEntry.FilePath, source, cancellationToken);
            return cacheEntry.FilePath;
        }
        finally
        {
            gate.Release();

            if (File.Exists(cacheEntry.FilePath))
            {
                _locks.TryRemove(cacheEntry.FileName, out _);
            }
        }
    }

    private static async Task DownloadImage(string destinationFilePath, Stream stream, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(CacheDirectoryPath);
        var tmpPath = Path.GetTempFileName();

        try
        {
            await using (var tmpFileStream = new FileStream(tmpPath, FileMode.Create,
                             FileAccess.Write, FileShare.None, 1024 * 32,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await stream.CopyToAsync(tmpFileStream, cancellationToken);
            }

            File.Move(tmpPath, destinationFilePath, true);
        }
        finally
        {
            TryDelete(tmpPath);
        }
    }

    private static string GetFileName(Uri uri)
    {
        const string extension = ".png";
        const int hashLength = 32;

        return string.Create(hashLength * 2 + extension.Length, uri, static (destination, uri) =>
        {
            ReadOnlySpan<char> text = uri.AbsoluteUri.AsSpan();

            int length = Encoding.UTF8.GetByteCount(text);
            bool useStackAlloc = length <= 512;

            using var bufferOwner = useStackAlloc
                ? SpanOwner<byte>.Empty
                : SpanOwner<byte>.Allocate(length);

            Span<byte> buffer = useStackAlloc
                ? stackalloc byte[length]
                : bufferOwner.Span;

            int written = Encoding.UTF8.GetBytes(text, buffer);

            Span<byte> hash = stackalloc byte[hashLength];
            if (!SHA256.TryHashData(buffer[..written], hash, out int hashWritten))
                throw new InvalidOperationException("SHA256 hashing failed.");

            if (!Convert.TryToHexString(hash, destination, out int charsWritten))
                throw new InvalidOperationException("Convert.TryToHexString failed.");

            extension.AsSpan().CopyTo(destination[charsWritten..]);
        });
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            //
        }
    }

    private static CacheEntry CreateCacheEntry(Uri iconUri)
    {
        string fileName = GetFileName(iconUri);
        string filePath = Path.Combine(CacheDirectoryPath, fileName);

        return new CacheEntry(fileName, filePath, File.Exists(filePath));
    }

    private readonly record struct CacheEntry(string FileName, string FilePath, bool Exists);
}
