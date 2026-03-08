namespace BitwaredApi.Models.Auth;

public sealed record TokenResponseModel(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    string? RefreshToken,
    string? Key,
    string? PrivateKey,
    string? TwoFactorToken,
    KdfConfigModel? Kdf,
    UserDecryptionOptionsModel? UserDecryptionOptions);

public sealed record AuthSession(
    string AccountId,
    string Email,
    DateTimeOffset AccessTokenExpiresAt,
    BitwardenEnvironment Environment,
    bool HasUserKey);

public sealed record PendingDeviceLogin(
    string RequestId,
    string AccessCode,
    string FingerprintPhrase,
    DateTimeOffset Expires,
    string Email);

public sealed record AuthRequestCreateResponse(
    string Id,
    string AccessCode,
    DateTimeOffset Expires);

public sealed record AuthRequestStatusResponse(
    bool Approved,
    bool Answered,
    bool Expired,
    string? EncryptedUserKey,
    DateTimeOffset? ResponseDate,
    string? RequestDeviceIdentifier,
    string? RequestIpAddress,
    string? RequestCountryName);
