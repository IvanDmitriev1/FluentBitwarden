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
