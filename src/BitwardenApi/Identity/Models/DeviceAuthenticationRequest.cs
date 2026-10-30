using BitwardenApi.Client;
namespace BitwardenApi.Identity.Models;


public sealed record DeviceAuthenticationRequest(
    BitwardenClientContext Context,
    string Email,
    string OneTimeAccessCode,
    AuthRequestId AuthRequestId = default,
    string Scope = "api offline_access");

