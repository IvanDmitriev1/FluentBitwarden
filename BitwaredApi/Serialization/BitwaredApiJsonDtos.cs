using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitwaredApi.Serialization;

internal sealed class PreloginRequestDto
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

internal sealed class PreloginResponseDto
{
    [JsonPropertyName("kdfSettings")]
    public KdfSettingsDto? KdfSettings { get; init; }

    [JsonPropertyName("kdf")]
    public int? Kdf { get; init; }

    [JsonPropertyName("kdfIterations")]
    public int? KdfIterations { get; init; }

    [JsonPropertyName("kdfMemory")]
    public int? KdfMemory { get; init; }

    [JsonPropertyName("kdfParallelism")]
    public int? KdfParallelism { get; init; }
}

internal sealed class KdfSettingsDto
{
    [JsonPropertyName("kdfType")]
    public int? KdfType { get; init; }

    [JsonPropertyName("iterations")]
    public int? Iterations { get; init; }

    [JsonPropertyName("memory")]
    public int? Memory { get; init; }

    [JsonPropertyName("parallelism")]
    public int? Parallelism { get; init; }
}

internal sealed class TokenSuccessResponseDto
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("AccessToken")]
    public string? AccessTokenPascal { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("TokenType")]
    public string? TokenTypePascal { get; init; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }

    [JsonPropertyName("ExpiresIn")]
    public int? ExpiresInPascal { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("RefreshToken")]
    public string? RefreshTokenPascal { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("privateKey")]
    public string? PrivateKey { get; init; }

    [JsonPropertyName("twoFactorToken")]
    public string? TwoFactorToken { get; init; }

    [JsonPropertyName("kdf")]
    public int? Kdf { get; init; }

    [JsonPropertyName("kdfIterations")]
    public int? KdfIterations { get; init; }

    [JsonPropertyName("kdfMemory")]
    public int? KdfMemory { get; init; }

    [JsonPropertyName("kdfParallelism")]
    public int? KdfParallelism { get; init; }

    [JsonPropertyName("userDecryptionOptions")]
    public UserDecryptionOptionsDto? UserDecryptionOptions { get; init; }
}

internal sealed class UserDecryptionOptionsDto
{
    [JsonPropertyName("hasMasterPassword")]
    public bool? HasMasterPassword { get; init; }

    [JsonPropertyName("masterPasswordUnlock")]
    public MasterPasswordUnlockDto? MasterPasswordUnlock { get; init; }
}

internal sealed class MasterPasswordUnlockDto
{
    [JsonPropertyName("salt")]
    public string? Salt { get; init; }

    [JsonPropertyName("kdf")]
    public KdfSettingsDto? Kdf { get; init; }

    [JsonPropertyName("masterKeyEncryptedUserKey")]
    public string? MasterKeyEncryptedUserKey { get; init; }
}

internal sealed class TokenFailureResponseDto
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }

    [JsonPropertyName("ErrorDescription")]
    public string? ErrorDescriptionPascal { get; init; }

    [JsonPropertyName("deviceVerificationRequest")]
    public bool? DeviceVerificationRequest { get; init; }

    [JsonPropertyName("twoFactorProviders2")]
    public Dictionary<string, Dictionary<string, JsonElement>?>? TwoFactorProviders2 { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("ssoEmail2faSessionToken")]
    public string? SsoEmail2FaSessionToken { get; init; }
}

internal sealed class AuthRequestCreateRequestDto
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("deviceIdentifier")]
    public string? DeviceIdentifier { get; init; }

    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; init; }

    [JsonPropertyName("type")]
    public int Type { get; init; }

    [JsonPropertyName("accessCode")]
    public string? AccessCode { get; init; }
}

internal sealed class AuthRequestCreateResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("creationDate")]
    public DateTimeOffset? CreationDate { get; init; }
}

internal sealed class AuthRequestPollResponseDto
{
    [JsonPropertyName("requestApproved")]
    public bool? RequestApproved { get; init; }

    [JsonPropertyName("responseDate")]
    public DateTimeOffset? ResponseDate { get; init; }

    [JsonPropertyName("creationDate")]
    public DateTimeOffset? CreationDate { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("requestDeviceIdentifier")]
    public string? RequestDeviceIdentifier { get; init; }

    [JsonPropertyName("requestIpAddress")]
    public string? RequestIpAddress { get; init; }

    [JsonPropertyName("requestCountryName")]
    public string? RequestCountryName { get; init; }
}

internal sealed class JwtTokenPayloadDto
{
    [JsonPropertyName("sub")]
    public string? Subject { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonExtensionData]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, JsonElement>? AdditionalClaims { get; } = new();
}
