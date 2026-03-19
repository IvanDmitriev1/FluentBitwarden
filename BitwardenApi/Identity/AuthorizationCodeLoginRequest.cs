namespace BitwardenApi.Identity;

public sealed record AuthorizationCodeLoginRequest(
    BitwardenClientContext Context,
    string Code,
    string RedirectUri,
    string CodeVerifier,
    string Scope = "api offline_access",
    string ClientId = "desktop");
