using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.Contracts.Shared;

public static class FilePathHelpers
{
    public static MemoryOwner<byte> ReadAllBytesOwner(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 512, FileOptions.SequentialScan);

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
