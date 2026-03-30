using CommunityToolkit.HighPerformance.Buffers;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Internal;

internal readonly ref struct TpmProtectedPayloadOwner
{
    private readonly int _ciphertextLength;
    private readonly MemoryOwner<byte> _nonceOwner;
    private readonly MemoryOwner<byte> _ciphertextOwner;
    private readonly MemoryOwner<byte> _tagOwner;

    private TpmProtectedPayloadOwner(
        MemoryOwner<byte> nonceOwner,
        MemoryOwner<byte> ciphertextOwner,
        MemoryOwner<byte> tagOwner,
        int ciphertextLength)
    {
        _nonceOwner = nonceOwner;
        _ciphertextOwner = ciphertextOwner;
        _tagOwner = tagOwner;
        _ciphertextLength = ciphertextLength;
    }

    public Span<byte> Nonce => _nonceOwner.Span[..TpmProtectedPayloadCodec.NonceSize];
    public Span<byte> Ciphertext => _ciphertextOwner.Span[.._ciphertextLength];
    public Span<byte> Tag => _tagOwner.Span[..TpmProtectedPayloadCodec.TagSize];

    public static TpmProtectedPayloadOwner Create(int ciphertextLength)
    {
        var nonceOwner = MemoryOwner<byte>.Allocate(TpmProtectedPayloadCodec.NonceSize);
        var ciphertextOwner = MemoryOwner<byte>.Allocate(ciphertextLength);
        var tagOwner = MemoryOwner<byte>.Allocate(TpmProtectedPayloadCodec.TagSize);

        RandomNumberGenerator.Fill(nonceOwner.Span[..TpmProtectedPayloadCodec.NonceSize]);

        return new TpmProtectedPayloadOwner(nonceOwner, ciphertextOwner, tagOwner, ciphertextLength);
    }

    public TpmProtectedPayload CreatePayload(ReadOnlySpan<byte> wrappedKey)
        => new(wrappedKey, Nonce, Tag, Ciphertext);

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(Nonce);
        CryptographicOperations.ZeroMemory(Ciphertext);
        CryptographicOperations.ZeroMemory(Tag);

        _nonceOwner.Dispose();
        _ciphertextOwner.Dispose();
        _tagOwner.Dispose();
    }
}
