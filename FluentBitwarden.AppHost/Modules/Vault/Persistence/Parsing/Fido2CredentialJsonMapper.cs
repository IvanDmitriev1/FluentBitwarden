using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;
using System.Text.Json;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

internal static class Fido2CredentialJsonMapper
{
    public static Fido2CredentialKeyType ReadKeyType(
        ref Utf8JsonReader reader,
        CipherKey key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static value => Fido2CredentialEnumExtensions.ParseKeyType(value));

    public static Fido2CredentialKeyAlgorithm ReadKeyAlgorithm(
        ref Utf8JsonReader reader,
        CipherKey key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static value => Fido2CredentialEnumExtensions.ParseKeyAlgorithm(value));

    public static Fido2CredentialKeyCurve ReadKeyCurve(
        ref Utf8JsonReader reader,
        CipherKey key,
        string propertyName)
        => EncryptedJsonValueReader.ReadRequired(ref reader, key,
            static value => Fido2CredentialEnumExtensions.ParseKeyCurve(value));

    public static byte[] ReadCredentialId(
        ref Utf8JsonReader reader,
        CipherKey key,
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
