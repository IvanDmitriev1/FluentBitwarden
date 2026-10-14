namespace BitwardenApi.Infrastructure.Cryptography.Enc;

public static class EncStringExtensions
{
    extension(in EncString value)
    {
        public string Decode(ReadOnlySpan<byte> key)
        {
            if (value.IsEmpty)
                return string.Empty;

            EncStringParts parts = value.CreateParts();
            return AesCbcHmac.Decrypt(in parts, key);
        }

        public int DecodeTo(ReadOnlySpan<byte> key, Span<byte> destination)
        {
            EncStringParts parts = value.CreateParts();
            return AesCbcHmac.DecryptTo(in parts, key, destination);
        }

        internal byte[] DecodeToArray(ReadOnlySpan<byte> key)
        {
            EncStringParts parts = value.CreateParts();
            return AesCbcHmac.DecryptToArray(in parts, key);
        }
    }

    public static string DecodeEncString(this ReadOnlySpan<byte> encodedUtf8, ReadOnlySpan<byte> key)
    {
        var layout = EncString.ParseEncodedLayoutOrThrow(encodedUtf8);
        Span<byte> dataBuffer = new byte[layout.DataLength];

        Span<byte> ivBuffer = layout.HasIv
            ? stackalloc byte[layout.IvLength]
            : default;
        Span<byte> macBuffer = layout.HasMac
            ? stackalloc byte[layout.MacLength]
            : default;

        EncStringParts parts = EncString.DecodeEncodedSegments(
            encodedUtf8,
            in layout,
            dataBuffer,
            ivBuffer,
            macBuffer);

        return AesCbcHmac.Decrypt(in parts, key);
    }

    public static int DecodeEncStringInPlace(this Span<byte> encodedUtf8, ReadOnlySpan<byte> key)
    {
        var layout = EncString.ParseEncodedLayoutOrThrow(encodedUtf8);

        Span<byte> dataBuffer = encodedUtf8[..layout.DataLength];
        Span<byte> ivBuffer = layout.HasIv
            ? stackalloc byte[layout.IvLength]
            : default;
        Span<byte> macBuffer = layout.HasMac
            ? stackalloc byte[layout.MacLength]
            : default;

        EncStringParts parts = EncString.DecodeEncodedSegments(
            encodedUtf8,
            in layout,
            dataBuffer,
            ivBuffer,
            macBuffer);

        return AesCbcHmac.DecryptTo(in parts, key, dataBuffer);
    }
}