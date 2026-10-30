namespace BitwardenApi.Identity.Models;


public sealed record WebAuthnLoginAssertionOptionsResult(
    WebAuthnLoginAssertionOptions Options,
    WebAuthnLoginAssertionOptionsToken Token);

