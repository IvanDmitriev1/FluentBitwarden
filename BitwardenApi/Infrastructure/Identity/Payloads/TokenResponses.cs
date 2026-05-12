using System.Text.Json.Serialization;
using BitwardenApi.Cryptography;
using BitwardenApi.Models;

namespace BitwardenApi.Infrastructure.Identity.Payloads;

internal record TokenRefreshSessionResponse
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

internal sealed record TokenAuthenticatedResponse : TokenRefreshSessionResponse
{
    [JsonPropertyName("privateKey")]
    public required EncryptedPrivateKey EncryptedPrivateKey { get; init; }

    [JsonPropertyName("userDecryptionOptions")]
    public required UserDecryptionOptions UserDecryptionOptions { get; init; }
}


internal sealed class TokenFailureResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("error_description")]
    public required string ErrorDescription { get; init; }

    [JsonPropertyName("deviceVerificationRequest")]
    public bool? DeviceVerificationRequest { get; init; }

    [JsonPropertyName("twoFactorProviders2")]
    public Dictionary<string, Dictionary<string, JsonElement>?>? TwoFactorProviders2 { get; init; }
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
    public required EncryptedUserKey MasterKeyEncryptedUserKey { get; init; }

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

