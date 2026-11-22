using System.Text.Json.Serialization;
using MemoryPack;

using BitwardenApi.Encoding;

namespace BitwardenApi.Primitives;

[MemoryPackable]
public readonly partial record struct OpenSshPublicKey(
    string RawKey,
    byte[] KeyBlob)
{

    public static OpenSshPublicKey Empty { get; } = new OpenSshPublicKey(string.Empty, []);
    public static OpenSshPublicKey CreateUnparsed(string rawKey) => new(rawKey, []);

    [MemoryPackIgnore]
    [JsonIgnore]
    public string Algorithm
    {
        get
        {
            var rawKey = RawKey.AsSpan();
            int separator = rawKey.IndexOf(' ');
            return separator > 0 ? rawKey[..separator].ToString() : string.Empty;
        }
    }

    public static bool TryParse(string rawKey, out OpenSshPublicKey publicKey)
    {
        publicKey = default;
        ReadOnlySpan<char> rawKeySpan = rawKey.AsSpan();

        if (rawKeySpan.IsEmpty)
            return false;

        int blocks = rawKeySpan.Count(' ');
        if (blocks < 1)
            return false;

        Span<Range> ranges = stackalloc Range[blocks + 1];
        int writtenSplit = rawKeySpan.Split(ranges, ' ');

        ReadOnlySpan<char> keySpan = rawKeySpan[ranges[1]];
        if (!Base64Extensions.TryConvertFromBase64Chars(keySpan, out var keyBlob))
            return false;

        publicKey = new OpenSshPublicKey(rawKey, keyBlob);
        return true;
    }
}
