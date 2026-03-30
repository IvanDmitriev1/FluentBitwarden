namespace FluentBitwarden.Modules.Security.Crypto;

internal static class Base64Decoder
{
    public static bool TryGetDecodedByteCount(ReadOnlySpan<char> source, out int decodedByteCount)
    {
        if (source.IsEmpty)
        {
            decodedByteCount = 0;
            return true;
        }

        if ((source.Length & 0x03) != 0)
        {
            decodedByteCount = 0;
            return false;
        }

        int paddingCount = 0;
        if (source[^1] == '=')
        {
            paddingCount = 1;

            if (source.Length > 1 && source[^2] == '=')
            {
                paddingCount = 2;
            }
        }

        ReadOnlySpan<char> unpaddedSource = paddingCount == 0
            ? source
            : source[..^paddingCount];

        if (unpaddedSource.IndexOf('=') >= 0)
        {
            decodedByteCount = 0;
            return false;
        }

        decodedByteCount = checked(source.Length / 4 * 3 - paddingCount);
        return true;
    }

    public static bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
    {
        return Convert.TryFromBase64Chars(source, destination, out bytesWritten);
    }
}
