namespace FluentBitwarden.Modules.Security.Crypto.Enc;

internal readonly ref struct EncStringParts(
    EncStringType type,
    ReadOnlySpan<char> data,
    ReadOnlySpan<char> iv = default,
    ReadOnlySpan<char> mac = default)
{
    public EncStringType Type { get; } = type;
    public ReadOnlySpan<char> Data { get; } = data;
    public ReadOnlySpan<char> Iv { get; } = iv;
    public ReadOnlySpan<char> Mac { get; } = mac;
}