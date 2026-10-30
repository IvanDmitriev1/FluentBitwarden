using BitwardenApi.Client;
namespace BitwardenApi.Identity.Models;


public sealed record AuthorizationCodeLoginRequest(
    BitwardenClientContext Context,
    string Code,
    string RedirectUri,
    string CodeVerifier,
    string Scope = "api offline_access");
