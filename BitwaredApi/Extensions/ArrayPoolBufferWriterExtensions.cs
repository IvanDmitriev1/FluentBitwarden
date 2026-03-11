using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Extensions;

internal static class ArrayPoolBufferWriterExtensions
{
    public static void CompactUnreadBytes(
        this ArrayPoolBufferWriter<byte> bufferWriter,
        int consumed,
        int remainingBytes)
    {
        if (remainingBytes <= 0)
        {
            bufferWriter.Clear();
            return;
        }

        using var unreadBytes = MemoryOwner<byte>.Allocate(remainingBytes);
        bufferWriter.WrittenSpan.Slice(consumed, remainingBytes).CopyTo(unreadBytes.Span);
        bufferWriter.Clear();
        unreadBytes.Span.CopyTo(bufferWriter.GetSpan(remainingBytes));
        bufferWriter.Advance(remainingBytes);
    }
}
