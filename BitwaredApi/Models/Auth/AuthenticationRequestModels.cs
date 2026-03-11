namespace BitwaredApi.Models.Auth;

public sealed record PasswordSignInRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPassword);

public sealed record TwoFactorSignInRequest(
    BitwardenClientContext Context,
    PasswordSignInContinuation Continuation,
    string Token,
    TwoFactorProviderType Provider,
    bool Remember);

public sealed record DeviceLoginStartRequest(
    BitwardenClientContext Context,
    string Email);

public sealed record DeviceLoginPollRequest(
    BitwardenClientContext Context,
    PendingDeviceLogin PendingRequest,
    DeviceSignInContinuation Continuation);
