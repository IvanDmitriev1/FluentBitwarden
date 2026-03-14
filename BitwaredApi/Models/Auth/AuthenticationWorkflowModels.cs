using System.Security.Cryptography;

namespace BitwaredApi.Models.Auth;

public sealed record AuthSession(
    string AccountId,
    string Email,
    DateTimeOffset AccessTokenExpiresAt,
    BitwardenEnvironment Environment,
    bool HasUserKey);

public sealed record PersistableSession(
    string AccountId,
    string Email,
    BitwardenEnvironment Environment,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string DeviceIdentifier,
    string? MasterKeyEncryptedUserKey,
    string? PrivateKey,
    string? MasterPasswordSalt,
    KdfConfigModel? KdfConfig)
{
    public bool CanUnlockWithMasterPassword
        => !string.IsNullOrWhiteSpace(MasterKeyEncryptedUserKey) && KdfConfig is not null;
}

public sealed record PendingDeviceLogin(
    string RequestId,
    string AccessCode,
    string FingerprintPhrase,
    DateTimeOffset Expires,
    string Email);

public sealed record DeviceLoginStartResult(
    PendingDeviceLogin Login,
    DeviceSignInContinuation Continuation);

public sealed record AuthenticationSuccess(
    AuthSession Session,
    PersistableSession PersistableSession,
    string AccessToken,
    byte[]? DecryptedUserKey);

public abstract record PasswordSignInOutcome
{
    private PasswordSignInOutcome() { }

    public sealed record Success(AuthenticationSuccess Authentication) : PasswordSignInOutcome;

    public sealed record TwoFactorRequired(
        TwoFactorChallenge Challenge,
        PasswordSignInContinuation Continuation) : PasswordSignInOutcome;

    public sealed record InvalidCredentials(string Message) : PasswordSignInOutcome;
    public sealed record DeviceVerificationRequired(string Message) : PasswordSignInOutcome;
}

public abstract record AuthenticationOutcome
{
    private AuthenticationOutcome() { }

    public sealed record Success(AuthenticationSuccess Authentication) : AuthenticationOutcome;
    public sealed record InvalidCredentials(string Message) : AuthenticationOutcome;
    public sealed record DeviceVerificationRequired(string Message) : AuthenticationOutcome;
}

public abstract record DeviceApprovalOutcome
{
    private DeviceApprovalOutcome() {}

    public sealed record Pending : DeviceApprovalOutcome;
    public sealed record Approved(AuthenticationSuccess Authentication) : DeviceApprovalOutcome;
    public sealed record Denied(string Message) : DeviceApprovalOutcome;
    public sealed record Expired(string Message) : DeviceApprovalOutcome;
}

public sealed class PasswordSignInContinuation : IDisposable
{
    internal PasswordSignInContinuation(string email, KdfConfigModel kdf, MasterPasswordAuth auth)
    {
        Email = email;
        Kdf = kdf;
        Auth = auth;
    }

    internal string Email { get; }
    internal KdfConfigModel Kdf { get; }
    internal MasterPasswordAuth Auth { get; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Auth.Dispose();
    }
}

public sealed class DeviceSignInContinuation : IDisposable
{
    internal DeviceSignInContinuation(string email, string accessCode, byte[] privateKeyPkcs8)
    {
        Email = email;
        AccessCode = accessCode;
        PrivateKeyPkcs8 = privateKeyPkcs8;
    }

    internal string Email { get; }
    internal string AccessCode { get; }
    internal byte[] PrivateKeyPkcs8 { get; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CryptographicOperations.ZeroMemory(PrivateKeyPkcs8);
    }
}
