using System.Text;

namespace BitwardenApi.Vault.Items.Contracts;

public static class Fido2CredentialEnumExtensions
{
    extension(Fido2CredentialKeyType value)
    {
        public string ToWireValue() => value switch
        {
            Fido2CredentialKeyType.PublicKey => "public-key",
            _ => throw new NotSupportedException($"Unsupported FIDO2 key type: {value}.")
        };
    }

    extension(Fido2CredentialKeyAlgorithm value)
    {
        public string ToWireValue() => value switch
        {
            Fido2CredentialKeyAlgorithm.Ecdsa => "ECDSA",
            _ => throw new NotSupportedException($"Unsupported FIDO2 key algorithm: {value}.")
        };
    }

    extension(Fido2CredentialKeyCurve value)
    {
        public string ToWireValue() => value switch
        {
            Fido2CredentialKeyCurve.P256 => "P-256",
            _ => throw new NotSupportedException($"Unsupported FIDO2 key curve: {value}.")
        };
    }

    public static Fido2CredentialKeyType ParseKeyType(ReadOnlySpan<byte> wireValue)
        => Ascii.EqualsIgnoreCase(wireValue, "public-key"u8)
            ? Fido2CredentialKeyType.PublicKey
            : throw new NotSupportedException("Unsupported FIDO2 key type.");

    public static Fido2CredentialKeyAlgorithm ParseKeyAlgorithm(ReadOnlySpan<byte> wireValue)
        => Ascii.EqualsIgnoreCase(wireValue, "ECDSA"u8)
            ? Fido2CredentialKeyAlgorithm.Ecdsa
            : throw new NotSupportedException("Unsupported FIDO2 key algorithm.");

    public static Fido2CredentialKeyCurve ParseKeyCurve(ReadOnlySpan<byte> wireValue)
        => Ascii.EqualsIgnoreCase(wireValue, "P-256"u8)
            ? Fido2CredentialKeyCurve.P256
            : throw new NotSupportedException("Unsupported FIDO2 key curve.");
}
