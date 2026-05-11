using System.Text;
using System.Text.Json;
using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Internal;

internal static class Fido2CredentialJsonMapper
{
    public static Fido2CredentialKeyType ReadKeyType(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key, propertyName,
            static (value, propertyName) =>
            {
                if (Ascii.EqualsIgnoreCase(value, "public-key"u8))
                {
                    return Fido2CredentialKeyType.PublicKey;
                }

                throw new JsonException($"{propertyName} contains an unsupported Fido2 credential key type.");
            });

    public static Fido2CredentialKeyAlgorithm ReadKeyAlgorithm(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key, propertyName,
            static (value, propertyName) =>
            {
                if (Ascii.EqualsIgnoreCase(value, "ECDSA"u8))
                {
                    return Fido2CredentialKeyAlgorithm.Ecdsa;
                }

                throw new JsonException($"{propertyName} contains an unsupported Fido2 credential key algorithm.");
            });

    public static Fido2CredentialKeyCurve ReadKeyCurve(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key, propertyName,
            static (value, propertyName) =>
            {
                if (Ascii.EqualsIgnoreCase(value, "P-256"u8))
                {
                    return Fido2CredentialKeyCurve.P256;
                }

                throw new JsonException($"{propertyName} contains an unsupported Fido2 credential key curve.");
            });

    public static byte[] ReadCredentialId(
        ref Utf8JsonReader reader,
        scoped ReadOnlySpan<byte> key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key, propertyName,
            static (value, propertyName) =>
            {
                if (Guid.TryParse(value, out Guid guid))
                {
                    return guid.ToByteArray(bigEndian: true);
                }

                throw new JsonException($"{propertyName} must be a valid GUID value.");
            });

}
