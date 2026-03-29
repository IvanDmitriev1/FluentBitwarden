using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Crypto;

internal static class Base64Decoder
{
    public static int GetDecodedByteCount(ReadOnlySpan<char> source, string sourceName)
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

        if (source[..^paddingCount].IndexOf('=') >= 0)
        {
            ThrowInvalidBase64(sourceName);
        }

        return checked(source.Length / 4 * 3 - paddingCount);
    }

    public static int Decode(ReadOnlySpan<char> source, Span<byte> destination, string sourceName)
    {
        if (!Convert.TryFromBase64Chars(source, destination, out int bytesWritten))
        {
            ThrowInvalidBase64(sourceName);
        }

        return bytesWritten;
    }

    private static void ThrowInvalidBase64(string sourceName)
        => throw new CryptographicException($"{sourceName} was not valid Base64.");
}
