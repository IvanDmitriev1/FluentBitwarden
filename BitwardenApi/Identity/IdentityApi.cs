using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using BitwardenApi.Identity;
using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Contracts;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Items;
using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Identity.Contracts;
using BitwardenApi.Infrastructure.Encoding;

namespace BitwardenApi.Identity;

internal sealed class IdentityApi(IHttpClientFactory httpClientFactory) : IIdentityApi
{
    public Task<TokenExchangeOutcome> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreatePasswordGrant(),
            "Identity login with password",
            IdentityJsonContext.ConfiguredDefault.TokenAuthenticatedResponse,
            static payload => new TokenExchangeOutcome.Authenticated(payload.ToTokenResponse()),
            cancellationToken);

    public Task<TokenExchangeOutcome> LoginWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreatePasswordWithTwoFactorGrant(),
            "Identity login with password and two-factor",
            IdentityJsonContext.ConfiguredDefault.TokenAuthenticatedResponse,
            static payload => new TokenExchangeOutcome.Authenticated(payload.ToTokenResponse()),
            cancellationToken);

    public async Task<WebAuthnLoginAssertionOptionsResult> GetWebAuthnLoginAssertionOptionsAsync(
        BitwardenClientContext context,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateIdentityClient();
        Uri endpoint = new(context.Environment.IdentityBase, "/accounts/webauthn/assertion-options");

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccess("Identity get WebAuthn assertion options", cancellationToken);

        WebAuthnLoginAssertionOptionsResponse? payload = await response.Content.ReadFromJsonAsync(
            IdentityJsonContext.ConfiguredDefault.WebAuthnLoginAssertionOptionsResponse,
            cancellationToken);

        if (payload is null)
            throw new InvalidDataException("Response JSON payload was empty.");

        return new WebAuthnLoginAssertionOptionsResult(payload.Options, payload.Token);
    }

    public Task<TokenExchangeOutcome> LoginWithWebAuthnAsync(
        WebAuthnLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateWebAuthnGrant(),
            "Identity login with passkey",
            IdentityJsonContext.ConfiguredDefault.TokenAuthenticatedResponse,
            static payload => new TokenExchangeOutcome.Authenticated(payload.ToTokenResponse()),
            cancellationToken);

    public Task<TokenExchangeOutcome> RefreshAsync(
        RefreshLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateRefreshTokenGrant(),
            "Identity refresh token",
            IdentityJsonContext.ConfiguredDefault.TokenRefreshSessionResponse,
            static payload => new TokenExchangeOutcome.SessionRefreshed(payload.ToTokenRefreshSessionModel()),
            cancellationToken);

    public Task<TokenExchangeOutcome> LoginWithDeviceAsync(
        DeviceLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateDeviceGrant(),
            "Identity login with device",
            IdentityJsonContext.ConfiguredDefault.TokenAuthenticatedResponse,
            static payload => new TokenExchangeOutcome.Authenticated(payload.ToTokenResponse()),
            cancellationToken);

    public Task<TokenExchangeOutcome> LoginWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateAuthorizationCodeGrant(),
            "Identity login with authorization code",
            IdentityJsonContext.ConfiguredDefault.TokenAuthenticatedResponse,
            static payload => new TokenExchangeOutcome.Authenticated(payload.ToTokenResponse()),
            cancellationToken);

    private async Task<TokenExchangeOutcome> SendTokenRequestAsync<TPayload>(
        BitwardenClientContext context,
        IReadOnlyDictionary<string, string> form,
        string operation,
        JsonTypeInfo<TPayload> payloadTypeInfo,
        Func<TPayload, TokenExchangeOutcome> successFactory,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateIdentityClient();
        Uri tokenEndpoint = new(context.Environment.IdentityBase, "/connect/token");

        using var content = new FormUrlEncodedContent(form);
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        request.Content = content;

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response is { IsSuccessStatusCode: false, StatusCode: HttpStatusCode.BadRequest })
        {
            var failureResponse = await response.Content.ReadFromJsonAsync(
                IdentityJsonContext.ConfiguredDefault.TokenFailureResponse,
                cancellationToken: cancellationToken);

            if (failureResponse is null)
                throw new InvalidDataException("Response JSON payload was empty.");

            return failureResponse.ToTokenFailureOutcome();
        }

        response.EnsureSuccess(operation, cancellationToken);

        TPayload? payload = await response.Content.ReadFromJsonAsync(
            payloadTypeInfo,
            cancellationToken);

        if (payload is null)
            throw new InvalidDataException("Response JSON payload was empty.");

        return successFactory(payload);
    }
}

