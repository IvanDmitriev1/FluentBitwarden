using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers.Text;

namespace FluentBitwarden.Modules.SshAgent.Models;

internal readonly record struct OpenSshPublicKey(
    OpenSshKeyAlgorithm Algorithm,
    byte[] KeyBlob)
{
    public static bool TryParse(ReadOnlySpan<char> line, out OpenSshPublicKey publicKey)
    {
        publicKey = default;

        if (line.IsEmpty)
            return false;

        int blocks = line.Count(' ');
        if (blocks < 1)
            return false;

        Span<Range> ranges = stackalloc Range[blocks + 1];
        int writtenSplit = line.Split(ranges, ' ');
        ReadOnlySpan<char> algorithmSpan = line[ranges[0]];
        if (!OpenSshKeyAlgorithmExtensions.TryParse(algorithmSpan, out var algorithm))
            return false;

        ReadOnlySpan<char> keySpan = line[ranges[1]];
        int maxDecodedLength = Base64.GetMaxDecodedFromUtf8Length(keySpan.Length);


        bool useStackAlloc = maxDecodedLength <= 256;

        using var decodedKeyBufferOwner = useStackAlloc
            ? SpanOwner<byte>.Allocate(maxDecodedLength)
            : SpanOwner<byte>.Empty;

        Span<byte> decodedKeyBuffer = useStackAlloc
            ? stackalloc byte[maxDecodedLength]
            : decodedKeyBufferOwner.Span;

        if (!Convert.TryFromBase64Chars(keySpan, decodedKeyBuffer, out var written))
            return false;

        publicKey = new OpenSshPublicKey(algorithm, decodedKeyBuffer[..written].ToArray());
        return true;
    }
}