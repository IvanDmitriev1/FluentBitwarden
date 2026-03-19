namespace BitwardenApi.Identity;

public sealed record PasswordLoginRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    string Scope = "api offline_access",
    string ClientId = "desktop");
