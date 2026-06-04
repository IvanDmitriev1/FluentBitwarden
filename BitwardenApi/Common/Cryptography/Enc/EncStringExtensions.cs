using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Cryptography.Enc;

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

        public int DecodeRsaTo(RSA privateKey, Span<byte> destination)
        {
            EncStringParts parts = value.CreateParts();
            return RsaOaep.DecryptTo(in parts, privateKey, destination);
        }
    }

    public static string DecodeEncString(this ReadOnlySpan<byte> encodedUtf8, ReadOnlySpan<byte> key)
    {
        var layout = EncString.ParseEncodedLayoutOrThrow(encodedUtf8);
        using var dataOwner = SpanOwner<byte>.Allocate(layout.DataLength);

        Span<byte> dataBuffer = dataOwner.Span;
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
