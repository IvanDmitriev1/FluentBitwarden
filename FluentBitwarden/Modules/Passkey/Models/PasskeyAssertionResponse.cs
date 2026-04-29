using FluentBitwarden.Modules.Passkey.Internal;
using System.Text.Json.Serialization;

namespace FluentBitwarden.Modules.Passkey.Models;

internal sealed class PasskeyAssertionResponse
{
    // Credential selected by the vault.
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] CredentialId { get; init; }

    // User handle from the original passkey credential.
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] UserId { get; init; }

    // Authenticator data bytes.
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] AuthenticatorData { get; init; }

    // Signature over authenticatorData || clientDataHash.
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] Signature { get; init; }

    public required string UserName { get; init; }
    public required string UserDisplayName { get; init; }
}
