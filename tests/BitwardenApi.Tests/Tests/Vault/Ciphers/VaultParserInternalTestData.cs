using System.Buffers;
using BitwardenApi.Vault.Ciphers.Serialization;
using BitwardenApi.Vault.Ciphers.Responses;

namespace BitwardenApi.Tests.Tests.Vault.Ciphers;

internal static partial class VaultParserTestData
{
    internal static Cipher ParseCipher(ref readonly CipherResponse dto, ReadOnlySpan<byte> payload, SymmetricCryptoKey key)
        => new CipherDataReader().Read(in dto, payload, key);

    internal static long MeasureAllocatedBytes(CipherDataReader parser, in CipherResponse dto, byte[] payload, SymmetricCryptoKey key)
    {
        for (int i = 0; i < 32; i++) _ = parser.Read(in dto, payload, key);
        const int iterations = 100;
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++) _ = parser.Read(in dto, payload, key);
        return (GC.GetAllocatedBytesForCurrentThread() - start) / iterations;
    }

    internal static T ParseEncryptedValueInPlace<T>(byte[] payload, ReadOnlySpan<byte> key, Span<byte> scratch, EncryptedJsonValueReader.DecryptedJsonValueParser<T> parser)
    {
        var reader = new Utf8JsonReader(payload);
        reader.Read(); reader.Read(); reader.Read();
        return EncryptedJsonValueReader.ParseInPlace(ref reader, key, scratch, parser);
    }

    internal static List<LoginUri> ReadLoginUriArray(LoginUrisJsonConverter converter, ReadOnlySpan<byte> payload, JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(payload);
        reader.Read();
        return converter.Read(ref reader, typeof(List<LoginUri>), options);
    }

    internal static byte[] CreateDirtyScratch(int length) => Enumerable.Repeat((byte)0xA5, length).ToArray();
    internal static void AssertScratchCleared(byte[] scratch) => Assert.All(scratch, static value => Assert.Equal((byte)0, value));

    internal static long MeasureConverterAllocations<T>(DecryptedValueJsonConverter<T> converter, byte[] payload, JsonSerializerOptions options)
    {
        for (int i = 0; i < 32; i++) _ = ReadConverterValue(converter, payload, options);
        long minimum = long.MaxValue;
        for (int sample = 0; sample < 5; sample++)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) _ = ReadConverterValue(converter, payload, options);
            minimum = Math.Min(minimum, GC.GetAllocatedBytesForCurrentThread() - start);
        }
        return minimum / 100;
    }

    internal static long MeasureUtf8StringAllocations(string plaintext)
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(plaintext);
        for (int i = 0; i < 32; i++) _ = System.Text.Encoding.UTF8.GetString(utf8.AsSpan());
        long minimum = long.MaxValue;
        for (int sample = 0; sample < 5; sample++)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) _ = System.Text.Encoding.UTF8.GetString(utf8.AsSpan());
            minimum = Math.Min(minimum, GC.GetAllocatedBytesForCurrentThread() - start);
        }
        return minimum / 100;
    }

    private static T ReadConverterValue<T>(DecryptedValueJsonConverter<T> converter, byte[] payload, JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(payload);
        reader.Read(); reader.Read(); reader.Read();
        return converter.Read(ref reader, typeof(T), options);
    }
}
