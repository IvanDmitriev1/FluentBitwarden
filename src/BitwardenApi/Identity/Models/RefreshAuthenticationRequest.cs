using BitwardenApi.Client;
namespace BitwardenApi.Identity.Models;


public sealed record RefreshAuthenticationRequest(
    BitwardenClientContext Context,
    SessionRefreshToken SessionRefreshToken,
    string Scope = "api offline_access");

