using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using BitwardenApi.Infrastructure.Cryptography.Enc;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

internal static class EncryptedJsonValueReader
{
    private const int MaxStackByteCount = 256;

    public delegate T DecryptedJsonValueParser<out T>(scoped Span<byte> value);

    public static T ParseEncryptedValue<T>(
        this ref Utf8JsonReader reader,
        ReadOnlySpan<byte> key,
        DecryptedJsonValueParser<T> parser)
    {
        int length = reader.ValueSpan.Length;
        if (length <= MaxStackByteCount)
        {
            Span<byte> scratch = stackalloc byte[length];
            return ParseInPlace(ref reader, key, scratch, parser);
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            return ParseInPlace(ref reader, key, rented.AsSpan(0, length), parser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rented);
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    internal static T ParseInPlace<T>(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        scoped Span<byte> scratch,
        DecryptedJsonValueParser<T> parser)
    {
        try
        {
            int bytesWritten = reader.CopyString(scratch);
            bytesWritten = scratch[..bytesWritten].DecodeEncStringInPlace(key);
            return parser(scratch[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scratch);
        }
    }
}
