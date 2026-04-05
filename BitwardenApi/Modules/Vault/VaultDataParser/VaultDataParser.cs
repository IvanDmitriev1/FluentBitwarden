using BitwardenApi.Cryptography;
using BitwardenApi.Cryptography.Enc;
using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Text;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    private const int MaxStackEncStringByteCount = 512;

    private static Utf8JsonReader CreateObjectReader(ReadOnlySpan<byte> payload)
    {
        var reader = new Utf8JsonReader(payload, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object payload.");
        }

        return reader;
    }

    private static void SkipValue(ref Utf8JsonReader reader)
    {
        reader.Read();

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            reader.Skip();
    }

    private static string? ReadDecryptField(ref Utf8JsonReader reader, scoped ReadOnlySpan<byte> key)
    {
        reader.Read();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        int length = reader.HasValueSequence
            ? checked((int)reader.ValueSequence.Length)
            : reader.ValueSpan.Length;
        bool useStackAlloc = length <= MaxStackEncStringByteCount;

        using var bufferOwner = useStackAlloc
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(length);

        Span<byte> buffer = useStackAlloc
            ? stackalloc byte[length]
            : bufferOwner.Span;

        int bytesWritten = reader.CopyString(buffer);
        var parts = EncString.Parse(buffer[..bytesWritten]);

        return CryptographyService.DecryptString(parts, key);
    }

    private static string DecryptField(ReadOnlySpan<char> encryptedValue, scoped ReadOnlySpan<byte> key)
    {
        int length = encryptedValue.Length;
        bool useStackAlloc = length <= MaxStackEncStringByteCount;

        using var bufferOwner = useStackAlloc
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(length);

        Span<byte> buffer = useStackAlloc
            ? stackalloc byte[length]
            : bufferOwner.Span;

        var status = Ascii.FromUtf16(encryptedValue, buffer, out int bytesWritten);
        if (status != OperationStatus.Done)
        {
            throw new FormatException("EncString contains non-ASCII characters.");
        }

        var parts = EncString.Parse(buffer[..bytesWritten]);
        return CryptographyService.DecryptString(parts, key);
    }
}
