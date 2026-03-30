using System.Buffers.Binary;

namespace FluentBitwarden.Modules.Security.Internal;

internal static class TpmProtectedPayloadCodec
{
    private const int Version = 1;
    private const int HeaderSize = sizeof(int) * 3;
    internal const int NonceSize = 12;
    internal const int TagSize = 16;

    public static void Write(Stream destination, in TpmProtectedPayload payload)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        BinaryPrimitives.WriteInt32LittleEndian(header, Version);
        BinaryPrimitives.WriteInt32LittleEndian(header[sizeof(int)..], payload.WrappedKey.Length);
        BinaryPrimitives.WriteInt32LittleEndian(header[(sizeof(int) * 2)..], payload.Ciphertext.Length);

        destination.Write(header);
        destination.Write(payload.WrappedKey);
        destination.Write(payload.Nonce);
        destination.Write(payload.Tag);
        destination.Write(payload.Ciphertext);
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> payload,
        out TpmProtectedPayload protectedPayload)
    {
        protectedPayload = default;

        if (payload.Length < HeaderSize + NonceSize + TagSize)
        {
            return false;
        }

        var version = BinaryPrimitives.ReadInt32LittleEndian(payload);
        var wrappedKeyLength = BinaryPrimitives.ReadInt32LittleEndian(payload[sizeof(int)..]);
        var ciphertextLength = BinaryPrimitives.ReadInt32LittleEndian(payload[(sizeof(int) * 2)..]);

        if (version != Version || wrappedKeyLength <= 0 || ciphertextLength <= 0)
        {
            return false;
        }

        var expectedLength = (long)HeaderSize + wrappedKeyLength + NonceSize + TagSize + ciphertextLength;
        if (expectedLength != payload.Length)
        {
            return false;
        }

        var offset = HeaderSize;

        var wrappedKey = payload[offset..(offset + wrappedKeyLength)];
        offset += wrappedKeyLength;

        var nonce = payload[offset..(offset + NonceSize)];
        offset += NonceSize;

        var tag = payload[offset..(offset + TagSize)];
        offset += TagSize;

        var ciphertext = payload[offset..(offset + ciphertextLength)];
        protectedPayload = new TpmProtectedPayload(wrappedKey, nonce, tag, ciphertext);
        return true;
    }
}
