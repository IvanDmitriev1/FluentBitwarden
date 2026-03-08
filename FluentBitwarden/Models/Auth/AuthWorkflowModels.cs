using BitwaredApi;

namespace FluentBitwarden.Models.Auth;

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
