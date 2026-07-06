using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

[JsonConverter(typeof(EncStringJsonConverter))]
public readonly struct EncString : IEquatable<EncString>
{
    public static readonly EncString Empty = new();

    private const int IvByteLength = 16;
    private const int MacByteLength = 32;
    private const int BlockByteLength = 16;

    private const int HeaderLength = 13;
    private const int MaxStackByteCount = 512;

    private readonly byte[] _bytes;
    private readonly SegmentLayout _layout;

    private EncString(byte[] bytes, SegmentLayout layout)
    {
        _bytes = bytes;
        _layout = layout;
    }

    public EncString()
    {
        _bytes = [];
        _layout = default;
    }

    public int MaxPlaintextByteCount => _layout.DataLength;
    public bool IsEmpty => _bytes is not { Length: > 0 };
    public byte[] ToByteArray() => _bytes ?? [];

    public bool Equals(EncString other)
    {
        if (IsEmpty)
            return other.IsEmpty;

        return !other.IsEmpty && _bytes.AsSpan().SequenceEqual(other._bytes);
    }
    public override bool Equals(object? obj) => obj is EncString other && Equals(other);
    public override int GetHashCode()
    {
        if (IsEmpty)
            return 0;

        var hashCode = new HashCode();
        foreach (byte value in _bytes)
        {
            hashCode.Add(value);
        }

        return hashCode.ToHashCode();
    }
    public static bool operator ==(EncString left, EncString right) => left.Equals(right);
    public static bool operator !=(EncString left, EncString right) => !left.Equals(right);

    internal EncStringParts CreateParts() => _layout.CreateParts(_bytes);

    /// <summary>
    /// <see cref="Utf8JsonWriter"/>'s segmented write methods (<c>WriteStringValueSegment</c>/
    /// <c>WriteBase64StringSegment</c>) cannot be mixed within one value — every segment of a
    /// value must use the same overload — so they don't fit this wire format, which alternates
    /// literal separators ("."/"|") with base64 chunks. A single pre-sized buffer is used instead.
    /// </summary>
    internal void WriteTo(Utf8JsonWriter writer)
    {
        if (IsEmpty)
        {
            writer.WriteNullValue();
            return;
        }

        EncStringParts parts = CreateParts();
        if (parts.Type is not (EncryptionType.AesCbc256_HmacSha256_B64 or EncryptionType.AesCbc256_B64))
            throw new NotSupportedException($"Encoding EncString type {parts.Type} is not supported.");

        int ivEncodedLength = Base64.GetMaxEncodedToUtf8Length(parts.Iv.Length);
        int dataEncodedLength = Base64.GetMaxEncodedToUtf8Length(parts.Data.Length);
        int macEncodedLength = parts.Mac.Length > 0 ? Base64.GetMaxEncodedToUtf8Length(parts.Mac.Length) : 0;
        int totalLength = 2 + ivEncodedLength + 1 + dataEncodedLength + (macEncodedLength > 0 ? 1 + macEncodedLength : 0);

        using var bufferOwner = totalLength <= MaxStackByteCount
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(totalLength);

        Span<byte> buffer = totalLength <= MaxStackByteCount
            ? stackalloc byte[totalLength]
            : bufferOwner.Span;

        int offset = 0;
        buffer[offset++] = (byte)('0' + (byte)parts.Type); // both supported types are single-digit (0, 2)
        buffer[offset++] = (byte)'.';

        Base64.EncodeToUtf8(parts.Iv, buffer[offset..], out _, out int ivWritten);
        offset += ivWritten;
        buffer[offset++] = (byte)'|';

        Base64.EncodeToUtf8(parts.Data, buffer[offset..], out _, out int dataWritten);
        offset += dataWritten;

        if (macEncodedLength > 0)
        {
            buffer[offset++] = (byte)'|';
            Base64.EncodeToUtf8(parts.Mac, buffer[offset..], out _, out int macWritten);
            offset += macWritten;
        }

        writer.WriteStringValue(buffer[..offset]);
    }

    public static EncString FromBytes(byte[] packedBytes)
    {
        SegmentLayout layout = ParsePackedLayout(packedBytes);
        return new EncString(packedBytes, layout);
    }

    public static EncString CreateFrom(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected JSON string token, got {reader.TokenType}.");

        int length = reader.ValueSpan.Length;
        bool useStackAlloc = length <= MaxStackByteCount;

        using var bufferOwner = useStackAlloc
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(length);

        Span<byte> buffer = useStackAlloc
            ? stackalloc byte[length]
            : bufferOwner.Span;

        int bytesWritten = reader.CopyString(buffer);
        var encodedUtf8 = buffer[..bytesWritten];

        SegmentLayout layout = ParseEncodedLayoutOrThrow(encodedUtf8);
        using var dataOwner = SpanOwner<byte>.Allocate(layout.DataLength);

        Span<byte> dataBuffer = dataOwner.Span;
        Span<byte> ivBuffer = layout.HasIv
            ? stackalloc byte[layout.IvLength]
            : default;
        Span<byte> macBuffer = layout.HasMac
            ? stackalloc byte[layout.MacLength]
            : default;

        EncStringParts parts = DecodeEncodedSegments(encodedUtf8, layout, dataBuffer, ivBuffer, macBuffer);
        byte[] bytes = Pack(in parts);

        return new EncString(bytes,
            SegmentLayout.CreatePacked(parts.Type, parts.Iv.Length, parts.Data.Length, parts.Mac.Length));
    }

    public static EncString Encrypt(string plaintext, ReadOnlySpan<byte> key)
    {
        int maxByteCount = System.Text.Encoding.UTF8.GetMaxByteCount(plaintext.Length);
        bool useStack = maxByteCount <= MaxStackByteCount;

        using var bufferOwner = useStack
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(maxByteCount);

        Span<byte> buffer = useStack
            ? stackalloc byte[maxByteCount]
            : bufferOwner.Span;

        try
        {
            int written = System.Text.Encoding.UTF8.GetBytes(plaintext, buffer);
            return Encrypt(buffer[..written], key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    public static EncString Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key)
    {
        int ciphertextCapacity = plaintext.Length + BlockByteLength;
        bool useStack = ciphertextCapacity <= MaxStackByteCount;

        using var ciphertextOwner = useStack
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(ciphertextCapacity);

        Span<byte> ciphertext = useStack
            ? stackalloc byte[ciphertextCapacity]
            : ciphertextOwner.Span;

        Span<byte> iv = stackalloc byte[IvByteLength];
        Span<byte> mac = stackalloc byte[MacByteLength];
        RandomNumberGenerator.Fill(iv);

        int ciphertextLength = AesCbcHmac.EncryptTo(plaintext, key, iv, ciphertext, mac);

        var parts = new EncStringParts(
            EncryptionType.AesCbc256_HmacSha256_B64,
            ciphertext[..ciphertextLength],
            iv,
            mac);

        byte[] bytes = Pack(in parts);
        return new EncString(
            bytes,
            SegmentLayout.CreatePacked(parts.Type, parts.Iv.Length, parts.Data.Length, parts.Mac.Length));
    }

    internal static SegmentLayout ParseEncodedLayoutOrThrow(ReadOnlySpan<byte> encodedUtf8)
    {
        if (!TryParseEncodedLayout(encodedUtf8, out var layout))
            throw new FormatException("The provided value is not a valid EncString.");

        return layout;
    }

    internal static EncStringParts DecodeEncodedSegments(
        ReadOnlySpan<byte> encodedUtf8,
        in SegmentLayout layout,
        Span<byte> dataBuffer,
        Span<byte> ivBuffer,
        Span<byte> macBuffer)
    {
        // Decode IV/MAC before data
        int ivLength = layout.HasIv
            ? DecodeEncodedSegmentInPlace(
                encodedUtf8.Slice(layout.IvOffset, layout.IvLength),
                ivBuffer,
                "IV")
            : 0;

        int macLength = layout.HasMac
            ? DecodeEncodedSegmentInPlace(
                encodedUtf8.Slice(layout.MacOffset, layout.MacLength),
                macBuffer,
                "MAC")
            : 0;

        int dataLength = DecodeEncodedSegmentInPlace(
            encodedUtf8.Slice(layout.DataOffset, layout.DataLength),
            dataBuffer,
            "data");

        return new EncStringParts(
            layout.Type,
            dataBuffer[..dataLength],
            ivBuffer[..ivLength],
            macBuffer[..macLength]);
    }

    private static SegmentLayout ParsePackedLayout(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength)
            throw new FormatException("The provided value is not a packed EncString.");

        EncryptionType type = (EncryptionType)bytes[0];
        int ivLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(1, 4));
        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(5, 4));
        int macLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(9, 4));

        if (ivLength < 0 || dataLength <= 0 || macLength < 0)
            throw new FormatException("The packed EncString lengths are invalid.");

        int bodyLength = ivLength + dataLength + macLength;
        if (bytes.Length != HeaderLength + bodyLength)
            throw new FormatException("The packed EncString length does not match its header.");

        return SegmentLayout.CreatePacked(type, ivLength, dataLength, macLength);
    }

    private static byte[] Pack(in EncStringParts parts)
    {
        int totalLength = checked(HeaderLength + parts.Iv.Length + parts.Data.Length + parts.Mac.Length);
        byte[] bytes = new byte[totalLength];
        Span<byte> destination = bytes;

        destination[0] = (byte)parts.Type;
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(1, 4), parts.Iv.Length);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(5, 4), parts.Data.Length);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(9, 4), parts.Mac.Length);

        int offset = HeaderLength;
        parts.Iv.CopyTo(destination[offset..]);
        offset += parts.Iv.Length;
        parts.Data.CopyTo(destination[offset..]);
        offset += parts.Data.Length;
        parts.Mac.CopyTo(destination[offset..]);

        return bytes;
    }

    private static int DecodeEncodedSegmentInPlace(
        ReadOnlySpan<byte> encoded,
        Span<byte> destination,
        string segmentName)
    {
        if (encoded.Length > destination.Length)
            throw new FormatException($"EncString {segmentName} segment exceeds destination buffer size.");

        Span<byte> buffer = destination[..encoded.Length];
        encoded.CopyTo(buffer);

        if (Base64.IsValid(buffer))
        {
            var status = Base64.DecodeFromUtf8InPlace(buffer, out int written);
            if (status == OperationStatus.Done)
                return written;
        }

        if (Base64Url.IsValid(buffer))
        {
            int written = Base64Url.DecodeFromUtf8InPlace(buffer);
            return written;
        }

        throw new FormatException($"EncString {segmentName} segment was not valid Base64/Base64Url.");
    }

    private static bool TryParseEncodedLayout(ReadOnlySpan<byte> value, out SegmentLayout layout)
    {
        layout = default;
        if (value.IsEmpty)
            return false;

        EncryptionType type = EncryptionType.AesCbc256_B64;
        int payloadOffset = 0;
        int dotIndex = value.IndexOf((byte)'.');
        if (dotIndex >= 0)
        {
            ReadOnlySpan<byte> typeToken = value[..dotIndex];
            if (!typeToken.IsEmpty &&
                Utf8Parser.TryParse(typeToken, out int typeValue, out int consumed) &&
                consumed == typeToken.Length)
            {
                type = (EncryptionType)typeValue;
                payloadOffset = dotIndex + 1;
            }
        }

        ReadOnlySpan<byte> payload = value[payloadOffset..];
        if (payload.IsEmpty)
            return false;

        Span<(int Offset, int Length)> segments = stackalloc (int Offset, int Length)[3];
        int segmentCount = 0;

        foreach (var segment in payload.Split((byte)'|'))
        {
            (int offset, int length) = segment.GetOffsetAndLength(payload.Length);
            segments[segmentCount++] = (payloadOffset + offset, length);
        }

        layout = (type, segmentCount) switch
        {
            (EncryptionType.AesCbc256_B64, 2) => new SegmentLayout(
                type, segments[1].Offset, segments[1].Length, segments[0].Offset, segments[0].Length, 0, 0),
            (EncryptionType.AesCbc256_HmacSha256_B64, 3) => new SegmentLayout(
                type, segments[1].Offset, segments[1].Length, segments[0].Offset, segments[0].Length,
                segments[2].Offset, segments[2].Length),
            (EncryptionType.Rsa2048_OaepSha1_B64 or EncryptionType.Rsa2048_OaepSha256_B64, 1) => new SegmentLayout(
                type, segments[0].Offset, segments[0].Length, 0, 0, 0, 0),
            (EncryptionType.Rsa2048_OaepSha1_HmacSha256_B64 or
                EncryptionType.Rsa2048_OaepSha256_HmacSha256_B64, 2) => new SegmentLayout(
                type, segments[0].Offset, segments[0].Length, 0, 0, segments[1].Offset, segments[1].Length),
            _ => default
        };

        return layout.DataLength > 0;
    }

    internal readonly record struct SegmentLayout(
        EncryptionType Type,
        int DataOffset,
        int DataLength,
        int IvOffset,
        int IvLength,
        int MacOffset,
        int MacLength)
    {
        public bool HasIv => IvLength > 0;
        public bool HasMac => MacLength > 0;

        public static SegmentLayout CreatePacked(EncryptionType type, int ivLength, int dataLength, int macLength)
        {
            int dataOffset = checked(HeaderLength + ivLength);
            int macOffset = checked(dataOffset + dataLength);
            return new SegmentLayout(type, dataOffset, dataLength, HeaderLength, ivLength, macOffset, macLength);
        }

        public EncStringParts CreateParts(ReadOnlySpan<byte> bytes) => new(
            Type,
            bytes.Slice(DataOffset, DataLength),
            bytes.Slice(IvOffset, IvLength),
            bytes.Slice(MacOffset, MacLength));
    }
}
