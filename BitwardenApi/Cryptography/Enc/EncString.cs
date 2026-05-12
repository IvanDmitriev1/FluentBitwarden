using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace BitwardenApi.Cryptography.Enc;

internal static class EncString
{
    public static EncStringParts Parse(Span<byte> buffer)
    {
        if (!TryParseLayout(buffer, out var layout))
            throw new FormatException("The provided value is not a valid EncString.");

        DecodeSegmentsInPlace(ref layout, buffer);
        return CreateParts(buffer, layout);
    }

    private static void DecodeSegmentsInPlace(ref PartsLayout layout, Span<byte> buffer)
    {
        layout.DataLength = DecodeEncodedSegmentInPlace(
            buffer.Slice(layout.DataOffset, layout.DataLength),
            "data");

        if (layout.HasIv)
        {
            layout.IvLength = DecodeEncodedSegmentInPlace(
                buffer.Slice(layout.IvOffset, layout.IvLength),
                "IV");
        }

        if (layout.HasMac)
        {
            layout.MacLength = DecodeEncodedSegmentInPlace(
                buffer.Slice(layout.MacOffset, layout.MacLength),
                "MAC");
        }
    }

    private static int DecodeEncodedSegmentInPlace(Span<byte> buffer, string segmentName)
    {
        if (Base64.IsValid(buffer))
        {
            var status = Base64.DecodeFromUtf8InPlace(buffer, out int written);
            if (status == OperationStatus.Done)
                return written;
        }

        if (Base64Url.IsValid(buffer))
        {
            return Base64Url.DecodeFromUtf8InPlace(buffer);
        }

#if DEBUG
        string raw = System.Text.Encoding.ASCII.GetString(buffer);
        throw new FormatException(
            $"EncString {segmentName} segment was not valid Base64/Base64Url. Raw segment: '{raw}'.");
#else
        throw new FormatException(
            $"EncString {segmentName} segment was not valid Base64/Base64Url.");
#endif
    }

    private static EncStringParts CreateParts(ReadOnlySpan<byte> buffer, PartsLayout layout)
    {
        ReadOnlySpan<byte> data = buffer.Slice(layout.DataOffset, layout.DataLength);
        ReadOnlySpan<byte> iv = layout.HasIv ? buffer.Slice(layout.IvOffset, layout.IvLength) : default;
        ReadOnlySpan<byte> mac = layout.HasMac ? buffer.Slice(layout.MacOffset, layout.MacLength) : default;
        return new EncStringParts(layout.Type, data, iv, mac);
    }

    private static bool TryParseLayout(ReadOnlySpan<byte> value, out PartsLayout layout)
    {
        layout = default;
        if (value.IsEmpty)
            return false;

        EncStringType type = EncStringType.AesCbc256_B64;
        int payloadOffset = 0;
        int dotIndex = value.IndexOf((byte)'.');
        if (dotIndex >= 0)
        {
            ReadOnlySpan<byte> typeToken = value[..dotIndex];
            if (!typeToken.IsEmpty && Utf8Parser.TryParse(typeToken, out int typeValue, out int consumed) &&
                consumed == typeToken.Length)
            {
                type = (EncStringType)typeValue;
                payloadOffset = dotIndex + 1;
            }
        }

        ReadOnlySpan<byte> payload = value[payloadOffset..];
        if (payload.IsEmpty)
            return false;

        int firstSep = payload.IndexOf((byte)'|');
        int secondSep = firstSep >= 0 ? payload[(firstSep + 1)..].IndexOf((byte)'|') : -1;
        if (secondSep >= 0)
        {
            secondSep += firstSep + 1;
        }

        if (secondSep >= 0 && payload[(secondSep + 1)..].IndexOf((byte)'|') >= 0)
            return false;

        int seg1Offset = payloadOffset;
        int seg1Length = firstSep < 0 ? payload.Length : firstSep;
        int seg2Offset = firstSep < 0 ? -1 : payloadOffset + firstSep + 1;
        int seg2Length = firstSep < 0 ? 0 : secondSep < 0 ? payload.Length - firstSep - 1 : secondSep - firstSep - 1;
        int seg3Offset = secondSep < 0 ? -1 : payloadOffset + secondSep + 1;
        int seg3Length = secondSep < 0 ? 0 : payload.Length - secondSep - 1;

        layout = type switch
        {
            EncStringType.AesCbc256_B64 when firstSep >= 0 && secondSep < 0 && seg2Length > 0 => new PartsLayout(
                type, seg2Offset, seg2Length, seg1Offset, seg1Length, -1, 0),
            EncStringType.AesCbc256_HmacSha256_B64 when firstSep >= 0 && secondSep >= 0 && seg2Length > 0 => new
                PartsLayout(type, seg2Offset, seg2Length, seg1Offset, seg1Length, seg3Offset, seg3Length),
            EncStringType.Rsa2048_OaepSha1_B64 or EncStringType.Rsa2048_OaepSha256_B64 when
                firstSep < 0 && seg1Length > 0 => new PartsLayout(type, seg1Offset, seg1Length, -1, 0, -1, 0),
            EncStringType.Rsa2048_OaepSha1_HmacSha256_B64 or EncStringType.Rsa2048_OaepSha256_HmacSha256_B64 when
                firstSep >= 0 && secondSep < 0 && seg1Length > 0 => new PartsLayout(type, seg1Offset, seg1Length,
                    -1, 0, seg2Offset, seg2Length),
            _ => default
        };

        return layout.DataLength > 0;
    }

    private struct PartsLayout(
        EncStringType type,
        int dataOffset,
        int dataLength,
        int ivOffset,
        int ivLength,
        int macOffset,
        int macLength)
    {
        public EncStringType Type = type;
        public int DataOffset = dataOffset;
        public int DataLength = dataLength;
        public int IvOffset = ivOffset;
        public int IvLength = ivLength;
        public int MacOffset = macOffset;
        public int MacLength = macLength;
        public bool HasIv => IvOffset >= 0;
        public bool HasMac => MacOffset >= 0;
    }
}
