using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

public sealed record DeviceLoginRequest(
    BitwardenClientContext Context,
    string Email,
    string OneTimeAccessCode,
    AuthRequestId? AuthRequestId = null,
    string Scope = "api offline_access",
    string ClientId = "desktop");
