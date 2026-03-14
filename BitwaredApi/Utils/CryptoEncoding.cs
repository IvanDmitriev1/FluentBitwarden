using System.Security.Cryptography;

namespace BitwaredApi.Utils;

internal static class CryptoEncoding
{
    public static int GetBase64DecodedLength(ReadOnlySpan<char> source, string sourceName)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        if ((source.Length & 0x03) != 0)
        {
            ThrowInvalidBase64(sourceName);
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

        if (source[..(source.Length - paddingCount)].IndexOf('=') >= 0)
        {
            ThrowInvalidBase64(sourceName);
        }

        return checked(((source.Length / 4) * 3) - paddingCount);
    }

    public static int DecodeBase64(ReadOnlySpan<char> source, Span<byte> destination, string sourceName)
    {
        if (!Convert.TryFromBase64Chars(source, destination, out int bytesWritten))
        {
            ThrowInvalidBase64(sourceName);
        }

        return bytesWritten;
    }

    public static char ToHexLower(int value)
        => (char)(value < 10 ? '0' + value : 'a' + (value - 10));

    private static void ThrowInvalidBase64(string sourceName)
        => throw new CryptographicException($"{sourceName} was not valid Base64.");
}
