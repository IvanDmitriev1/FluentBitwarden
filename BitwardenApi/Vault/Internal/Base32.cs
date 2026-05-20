namespace BitwardenApi.Vault.Internal;

internal sealed class Base32
{
    /// <summary>
    /// Safe upper bound for RFC 4648 Base32 decoded bytes.
    /// </summary>
    /// <param name="encodedLength"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int GetMaxDecodedLength(int encodedLength)
    {
        if (encodedLength < 0)
            throw new ArgumentOutOfRangeException(nameof(encodedLength));

        return checked(encodedLength * 5 / 8);
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten)
    {
        bytesWritten = 0;

        uint bitBuffer = 0;
        int bitCount = 0;

        int dataCharCount = 0;
        int paddingCount = 0;
        bool seenPadding = false;

        foreach (byte c in source)
        {
            if (c == (byte)'=')
            {
                seenPadding = true;
                paddingCount++;
                continue;
            }

            if (seenPadding)
            {
                // Only trailing '=' padding is allowed.
                bytesWritten = 0;
                return false;
            }

            int value = c switch
            {
                >= (byte)'A' and <= (byte)'Z' => c - (byte)'A',
                >= (byte)'a' and <= (byte)'z' => c - (byte)'a',
                >= (byte)'2' and <= (byte)'7' => c - (byte)'2' + 26,
                _ => -1
            };

            if (value < 0)
            {
                bytesWritten = 0;
                return false;
            }

            dataCharCount++;
            bitBuffer = (bitBuffer << 5) | (uint)value;
            bitCount += 5;

            while (bitCount >= 8)
            {
                bitCount -= 8;

                if ((uint)bytesWritten >= (uint)destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                destination[bytesWritten++] = (byte)(bitBuffer >> bitCount);
                bitBuffer &= bitCount == 0 ? 0u : (1u << bitCount) - 1;
            }
        }

        // Valid RFC 4648 Base32 data lengths modulo 8 are:
        // 0, 2, 4, 5, 7
        int mod = dataCharCount % 8;
        int expectedPadding = mod switch
        {
            0 => 0,
            2 => 6,
            4 => 4,
            5 => 3,
            7 => 1,
            _ => -1
        };

        if (expectedPadding < 0)
        {
            bytesWritten = 0;
            return false;
        }

        // If padding is present, require the exact RFC 4648 amount.
        if (paddingCount != 0 && paddingCount != expectedPadding)
        {
            bytesWritten = 0;
            return false;
        }

        // Leftover bits must be zero.
        if (bitCount > 0 && bitBuffer != 0)
        {
            bytesWritten = 0;
            return false;
        }

        return true;
    }
}
