using System.Text.Json.Serialization;
using BitwardenApi.Identity.Internal;

namespace BitwardenApi.Identity.Contracts;

internal record IdentityTokenRefreshSessionResponse
{
    [JsonPropertyName("access_token")]
    public required AccessToken AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public required RefreshToken RefreshToken { get; init; }

    [JsonPropertyName("twoFactorToken")]
    public TwoFactorToken? TwoFactorToken { get; init; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresInSeconds { get; init; }
}

internal sealed record IdentityTokenAuthenticatedResponse : IdentityTokenRefreshSessionResponse
{
    [JsonPropertyName("privateKey")]
    public required EncString ProtectedPrivateKey { get; init; }

    [JsonPropertyName("userDecryptionOptions")]
    public required UserDecryptionOptions UserDecryptionOptions { get; init; }
}


internal sealed record IdentityTokenFailureResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("error_description")]
    public required string ErrorDescription { get; init; }

    [JsonPropertyName("deviceVerificationRequest")]
    public bool? DeviceVerificationRequest { get; init; }

    [JsonPropertyName("twoFactorProviders2")]
    [JsonConverter(typeof(IdentityTwoFactorProviders2JsonConverter))]
    public required IReadOnlyList<IdentityTwoFactorProviderOption> TwoFactorProviders2 { get; init; }
}

internal sealed class UserDecryptionOptions
{
    [JsonPropertyName("masterPasswordUnlock")]
    public required MasterPasswordUnlock MasterPasswordUnlock { get; init; }
}

internal sealed class MasterPasswordUnlock
{
    [JsonPropertyName("kdf")]
    public required KdfSettingsDto Kdf { get; init; }

    [JsonPropertyName("masterKeyEncryptedUserKey")]
    public required EncString MasterKeyEncryptedUserKey { get; init; }

    [JsonPropertyName("salt")]
    public required string Salt { get; init; }
}



internal sealed class KdfSettingsDto
{
    [JsonPropertyName("kdfType")]
    public required KdfType KdfType { get; init; }

    [JsonPropertyName("iterations")]
    public required int Iterations { get; init; }

    [JsonPropertyName("memory")]
    public int? Memory { get; init; }

    [JsonPropertyName("parallelism")]
    public int? Parallelism { get; init; }
}


