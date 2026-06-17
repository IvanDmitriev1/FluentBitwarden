using System.Buffers.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Infrastructure.Encoding;

public static class Base64Extensions
{
    public static bool TryConvertFromBase64Chars(ReadOnlySpan<char> text, out byte[] bytes)
    {
        bytes = [];
        int maxDecodedLength = Base64.GetMaxDecodedFromUtf8Length(text.Length);
        bool useStackAlloc = maxDecodedLength <= 256;

        using var decodedKeyBufferOwner = useStackAlloc
            ? SpanOwner<byte>.Allocate(maxDecodedLength)
            : SpanOwner<byte>.Empty;

        Span<byte> decodedKeyBuffer = useStackAlloc
            ? stackalloc byte[maxDecodedLength]
            : decodedKeyBufferOwner.Span;

        if (!Convert.TryFromBase64Chars(text, decodedKeyBuffer, out var written))
            return false;

        bytes = decodedKeyBuffer[..written].ToArray();
        return true;
    }
}