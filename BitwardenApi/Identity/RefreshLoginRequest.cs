using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

public sealed record RefreshLoginRequest(
    BitwardenClientContext Context,
    RefreshToken RefreshToken,
    string Scope = "api offline_access",
    string ClientId = "desktop");
