using BitwardenApi.Client;
namespace BitwardenApi.Identity.Models;


public sealed record WebAuthnAuthenticationRequest(
    BitwardenClientContext Context,
    WebAuthnLoginAssertionOptionsToken Token,
    WebAuthnLoginAssertionResponseRequest DeviceResponse,
    string Scope = "api offline_access");

