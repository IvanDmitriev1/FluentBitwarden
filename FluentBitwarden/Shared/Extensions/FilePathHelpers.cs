using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.Shared.Extensions;

internal static class FilePathHelpers
{
    public static void EnsureParentDirectoryExists(string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static MemoryOwner<byte> ReadAllBytesOwner(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 256, FileOptions.SequentialScan);

        var length = (int)stream.Length;
        var owner = MemoryOwner<byte>.Allocate(length);

        try
        {
            stream.ReadExactly(owner.Span[..length]);
            return owner;
        }
        catch
        {
            owner.Span[..length].Clear();
            owner.Dispose();
            throw;
        }
    }
}
