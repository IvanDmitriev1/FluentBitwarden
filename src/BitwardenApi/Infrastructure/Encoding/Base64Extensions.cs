using System.Buffers.Text;

namespace BitwardenApi.Infrastructure.Encoding;

public static class Base64Extensions
{
    public static bool TryConvertFromBase64Chars(ReadOnlySpan<char> text, out byte[] bytes)
    {
        bytes = [];
        if (!Base64.IsValid(text, out int decodedLength))
            return false;

        var decodedBytes = new byte[decodedLength];
        if (!Convert.TryFromBase64Chars(text, decodedBytes, out int written) || written != decodedLength)
            return false;

        bytes = decodedBytes;
        return true;
    }
}