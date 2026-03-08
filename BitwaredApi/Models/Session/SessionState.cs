using BitwaredApi.Models.Auth;

namespace BitwaredApi.Models.Session;

public sealed record SessionState(
    string AccountId,
    string Email,
    string ApiBase,
    string IdentityBase,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string ClientId,
    string DeviceIdentifier,
    string? MasterKeyEncryptedUserKey,
    string? PrivateKey,
    string? MasterPasswordSalt,
    KdfConfigModel? KdfConfig);
