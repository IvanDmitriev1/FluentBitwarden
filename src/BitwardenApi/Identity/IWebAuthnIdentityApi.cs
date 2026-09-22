namespace BitwardenApi.Identity;

public interface IWebAuthnIdentityApi
{
    Task<WebAuthnLoginAssertionOptionsResult> GetAuthenticationAssertionOptionsAsync(
        BitwardenClientContext context,
        CancellationToken cancellationToken = default);
}
