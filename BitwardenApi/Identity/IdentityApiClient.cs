using System.Net.Http.Headers;
using System.Net.Http.Json;
using BitwardenApi.Context;
using BitwardenApi.Internal;
using BitwardenApi.Identity.Internal;

namespace BitwardenApi.Identity;

public sealed class IdentityApiClient(HttpClient httpClient) : IIdentityApiClient
{
    public Task<TokenResponse> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreatePasswordGrant(),
            "Identity login with password",
            cancellationToken);

    public Task<TokenResponse> LoginWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreatePasswordWithTwoFactorGrant(),
            "Identity login with password and two-factor",
            cancellationToken);

    public Task<TokenResponse> RefreshAsync(
        RefreshLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateRefreshTokenGrant(),
            "Identity refresh token",
            cancellationToken);

    public Task<TokenResponse> LoginWithDeviceAsync(
        DeviceLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateDeviceGrant(),
            "Identity login with device",
            cancellationToken);

    public Task<TokenResponse> LoginWithClientCredentialsAsync(
        ClientCredentialsLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateClientCredentialsGrant(),
            "Identity login with client credentials",
            cancellationToken);

    public Task<TokenResponse> LoginWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateAuthorizationCodeGrant(),
            "Identity login with authorization code",
            cancellationToken);

    private async Task<TokenResponse> SendTokenRequestAsync(
        BitwardenClientContext context,
        IReadOnlyDictionary<string, string> form,
        string operation,
        CancellationToken cancellationToken)
    {
        Uri tokenEndpoint = new(context.Environment.IdentityBase, "/connect/token");
        using FormUrlEncodedContent content = new(form);
        using HttpRequestMessage request = new(HttpMethod.Post, tokenEndpoint);
        request.Content = content;

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess(operation, cancellationToken);

        TokenResponse? payload = await response.Content.ReadFromJsonAsync<TokenResponse>(
            BitwardenApiJsonContext.Default.Options,
            cancellationToken);

        if (payload is null)
        {
            throw new InvalidDataException("Response JSON payload was empty.");
        }

        if (string.IsNullOrWhiteSpace(payload.AccessToken.Value))
        {
            throw new InvalidDataException("Identity token response did not include access_token.");
        }

        return payload;
    }
}
