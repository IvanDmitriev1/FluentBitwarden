using System.Net.Http.Json;

namespace BitwardenApi.Identity;

internal sealed class WebAuthnIdentityApi(IHttpClientFactory httpClientFactory) : IWebAuthnIdentityApi
{
    public async Task<WebAuthnLoginAssertionOptionsResult> GetAuthenticationAssertionOptionsAsync(
        BitwardenClientContext context,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateIdentityClient();
        Uri endpoint = new(context.Environment.IdentityBase, "/accounts/webauthn/assertion-options");

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccess("Identity get WebAuthn assertion options", cancellationToken);

        WebAuthnAuthenticationAssertionOptionsResponse? payload = await response.Content.ReadFromJsonAsync(
            IdentityJsonContext.ConfiguredDefault.WebAuthnAuthenticationAssertionOptionsResponse,
            cancellationToken);

        if (payload is null)
            throw new InvalidDataException("Response JSON payload was empty.");

        return new WebAuthnLoginAssertionOptionsResult(payload.Options, payload.Token);
    }
}
