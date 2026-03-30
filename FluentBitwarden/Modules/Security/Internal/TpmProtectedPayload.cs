namespace FluentBitwarden.Modules.Security.Internal;

internal readonly ref struct TpmProtectedPayload(
    ReadOnlySpan<byte> wrappedKey,
    ReadOnlySpan<byte> nonce,
    ReadOnlySpan<byte> tag,
    ReadOnlySpan<byte> ciphertext)
{
    public ReadOnlySpan<byte> WrappedKey { get; } = wrappedKey;
    public ReadOnlySpan<byte> Nonce { get; } = nonce;
    public ReadOnlySpan<byte> Tag { get; } = tag;
    public ReadOnlySpan<byte> Ciphertext { get; } = ciphertext;
}