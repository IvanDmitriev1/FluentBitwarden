using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;
using System.Text;
using System.Text.Json;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

internal static class Fido2CredentialJsonMapper
{
    public static Fido2CredentialKeyType ReadKeyType(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static value =>
            {
                if (Ascii.EqualsIgnoreCase(value, "public-key"u8))
                {
                    return Fido2CredentialKeyType.PublicKey;
                }

                throw new JsonException($"Property contains an unsupported Fido2 credential key type.");
            });

    public static Fido2CredentialKeyAlgorithm ReadKeyAlgorithm(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static (value) =>
            {
                if (Ascii.EqualsIgnoreCase(value, "ECDSA"u8))
                {
                    return Fido2CredentialKeyAlgorithm.Ecdsa;
                }

                throw new JsonException("Property contains an unsupported Fido2 credential key algorithm.");
            });

    public static Fido2CredentialKeyCurve ReadKeyCurve(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static value =>
            {
                if (Ascii.EqualsIgnoreCase(value, "P-256"u8))
                {
                    return Fido2CredentialKeyCurve.P256;
                }

                throw new JsonException("Property contains an unsupported Fido2 credential key curve.");
            });

    public static byte[] ReadCredentialId(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static value =>
            {
                if (Guid.TryParse(value, out Guid guid))
                {
                    return guid.ToByteArray(bigEndian: true);
                }

                throw new JsonException("Property must be a valid GUID value.");
            });

}
