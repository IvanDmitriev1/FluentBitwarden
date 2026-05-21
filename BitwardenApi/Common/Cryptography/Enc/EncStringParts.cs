namespace BitwardenApi.Cryptography.Enc;

internal readonly ref struct EncStringParts(
    EncStringType type,
    ReadOnlySpan<byte> data,
    ReadOnlySpan<byte> iv = default,
    ReadOnlySpan<byte> mac = default)
{
    public EncStringType Type { get; } = type;
    public ReadOnlySpan<byte> Data { get; } = data;
    public ReadOnlySpan<byte> Iv { get; } = iv;
    public ReadOnlySpan<byte> Mac { get; } = mac;
}
