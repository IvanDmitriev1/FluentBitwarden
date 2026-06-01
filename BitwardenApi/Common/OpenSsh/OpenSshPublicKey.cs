using BitwardenApi.Extensions;
using MemoryPack;

namespace BitwardenApi.OpenSsh;

[MemoryPackable]
public readonly partial record struct OpenSshPublicKey(
    string RawKey,
    byte[] KeyBlob)
{

    public static OpenSshPublicKey Empty { get; } = new OpenSshPublicKey(string.Empty, []);
    public static OpenSshPublicKey CreateUnparsed(string rawKey) => new(rawKey, []);

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
        ReadOnlySpan<char> algorithmSpan = rawKeySpan[ranges[0]];

        ReadOnlySpan<char> keySpan = rawKeySpan[ranges[1]];
        if (!Base64Extensions.TryConvertFromBase64Chars(keySpan, out var keyBlob))
            return false;

        publicKey = new OpenSshPublicKey(rawKey, keyBlob);
        return true;
    }
}