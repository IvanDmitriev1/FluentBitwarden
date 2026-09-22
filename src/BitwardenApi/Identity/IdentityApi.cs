using System.Net;
using System.Net.Http.Json;
using BitwardenApi.Identity.Internal;

namespace BitwardenApi.Identity;

internal sealed class IdentityApi(IHttpClientFactory httpClientFactory) : IIdentityApi
{
    public Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithPasswordAsync(
        PasswordAuthenticationRequest request,
        CancellationToken cancellationToken = default)
        => SendAuthenticatedTokenRequestAsync(
            request.Context, request.CreatePasswordGrant(), cancellationToken);

    public Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorAuthenticationRequest request,
        CancellationToken cancellationToken = default)
        => SendAuthenticatedTokenRequestAsync(
            request.Context, request.CreatePasswordWithTwoFactorGrant(), cancellationToken);

    public Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithWebAuthnAsync(
        WebAuthnAuthenticationRequest request,
        CancellationToken cancellationToken = default)
        => SendAuthenticatedTokenRequestAsync(
            request.Context, request.CreateWebAuthnGrant(), cancellationToken);

    public Task<SessionTokenResult<TokenRefreshSessionModel>> RefreshAuthenticationAsync(
        RefreshAuthenticationRequest request,
        CancellationToken cancellationToken = default)
        => SendRefreshTokenRequestAsync(
            request.Context, request.CreateRefreshTokenGrant(), cancellationToken);

    public Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithDeviceAsync(
        DeviceAuthenticationRequest request,
        CancellationToken cancellationToken = default)
        => SendAuthenticatedTokenRequestAsync(
            request.Context, request.CreateDeviceGrant(), cancellationToken);

    public Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendAuthenticatedTokenRequestAsync(
            request.Context, request.CreateAuthorizationCodeGrant(), cancellationToken);

    private Task<SessionTokenResult<TokenAuthenticatedModel>> SendAuthenticatedTokenRequestAsync(
        BitwardenClientContext context,
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
        => SendTokenRequestAsync(
            context, form, ReadAuthenticatedTokenAsync, cancellationToken);

    private Task<SessionTokenResult<TokenRefreshSessionModel>> SendRefreshTokenRequestAsync(
        BitwardenClientContext context,
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
        => SendTokenRequestAsync(
            context, form, ReadRefreshTokenAsync, cancellationToken);

    private static async Task<TokenAuthenticatedModel> ReadAuthenticatedTokenAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        IdentityTokenAuthenticatedResponse? payload = await content.ReadFromJsonAsync(
            IdentityJsonContext.ConfiguredDefault.IdentityTokenAuthenticatedResponse,
            cancellationToken);

        if (payload is null)
            throw new InvalidDataException("Response JSON payload was empty.");

        return payload.ToTokenResponse();
    }

    private static async Task<TokenRefreshSessionModel> ReadRefreshTokenAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        IdentityTokenRefreshSessionResponse? payload = await content.ReadFromJsonAsync(
            IdentityJsonContext.ConfiguredDefault.IdentityTokenRefreshSessionResponse,
            cancellationToken);

        if (payload is null)
            throw new InvalidDataException("Response JSON payload was empty.");

        return payload.ToTokenRefreshSessionModel();
    }

    private async Task<SessionTokenResult<TResult>> SendTokenRequestAsync<TResult>(
        BitwardenClientContext context,
        IReadOnlyDictionary<string, string> form,
        Func<HttpContent, CancellationToken, Task<TResult>> readSuccessAsync,
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
                                      IdentityJsonContext.ConfiguredDefault.IdentityTokenFailureResponse,
                                      cancellationToken: cancellationToken) ??
                                  throw new InvalidDataException("Response JSON payload was empty.");

            return new SessionTokenResult<TResult>.Rejected(failureResponse.ToTokenRejection());
        }

        response.EnsureSuccessStatusCode();
        return new SessionTokenResult<TResult>.Success(await readSuccessAsync(response.Content, cancellationToken));
    }
}

