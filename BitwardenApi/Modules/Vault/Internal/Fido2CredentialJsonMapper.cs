using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.Internal;

internal static class Fido2CredentialJsonMapper
{
    public static Fido2CredentialKeyType ParseKeyType(string value)
    {
        if (value.Equals("public-key", StringComparison.OrdinalIgnoreCase))
        {
            return Fido2CredentialKeyType.PublicKey;
        }

        throw new JsonException($"Unsupported Fido2 credential key type '{value}'.");
    }

    public static Fido2CredentialKeyAlgorithm ParseKeyAlgorithm(string value)
    {
        if (value.Equals("ECDSA", StringComparison.OrdinalIgnoreCase))
        {
            return Fido2CredentialKeyAlgorithm.Ecdsa;
        }

        throw new JsonException($"Unsupported Fido2 credential key algorithm '{value}'.");
    }

    public static Fido2CredentialKeyCurve ParseKeyCurve(string value)
    {
        if (value.Equals("P-256", StringComparison.OrdinalIgnoreCase))
        {
            return Fido2CredentialKeyCurve.P256;
        }

        throw new JsonException($"Unsupported Fido2 credential key curve '{value}'.");
    }
}
