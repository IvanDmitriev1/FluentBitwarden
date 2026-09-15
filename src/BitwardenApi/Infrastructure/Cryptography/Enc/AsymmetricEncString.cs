using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

/// <summary>
/// An RSA-wrapped encrypted value (Bitwarden EncryptionType 3-6, the "asymmetric EncString" /
/// sdk-internal <c>UnsignedSharedKey</c>). Decrypted with an RSA private key, unlike the symmetric
/// <see cref="EncString"/>. Used to wrap an organization key with the member's public key.
/// </summary>
[JsonConverter(typeof(AsymmetricEncStringJsonConverter))]
public readonly struct AsymmetricEncString : IEquatable<AsymmetricEncString>
{
    private readonly EncString _inner;

    private AsymmetricEncString(EncString inner) => _inner = inner;

    public static readonly AsymmetricEncString Empty = new(EncString.Empty);

    public int MaxPlaintextByteCount => _inner.MaxPlaintextByteCount;
    public bool IsEmpty => _inner.IsEmpty;
    public byte[] ToByteArray() => _inner.ToByteArray();

    public static AsymmetricEncString FromBytes(byte[] packedBytes) => new(EncString.FromBytes(packedBytes));

    internal static AsymmetricEncString FromEncString(EncString value) => new(value);

    public int DecodeRsaTo(RSA privateKey, Span<byte> destination)
    {
        EncStringParts parts = _inner.CreateParts();
        return RsaOaep.DecryptTo(in parts, privateKey, destination);
    }

    internal byte[] DecodeRsa(RSA privateKey)
    {
        EncStringParts parts = _inner.CreateParts();
        return RsaOaep.Decrypt(in parts, privateKey);
    }

    public bool Equals(AsymmetricEncString other) => _inner.Equals(other._inner);
    public override bool Equals(object? obj) => obj is AsymmetricEncString other && Equals(other);
    public override int GetHashCode() => _inner.GetHashCode();
    public static bool operator ==(AsymmetricEncString left, AsymmetricEncString right) => left.Equals(right);
    public static bool operator !=(AsymmetricEncString left, AsymmetricEncString right) => !left.Equals(right);
}
