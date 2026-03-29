using System.Net;
using System.Net.Http.Json;
using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Internal;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using BitwardenApi.Shared.Serialization;
using BitwardenApi.Shared.Transport;

namespace BitwardenApi.Modules.Identity.Services;

public sealed class IdentityApiClient(HttpClient httpClient) : IIdentityApiClient
{
    public Task<TokenExchangeOutcome> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreatePasswordGrant(),
            "Identity login with password",
            cancellationToken);

    public Task<TokenExchangeOutcome> LoginWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreatePasswordWithTwoFactorGrant(),
            "Identity login with password and two-factor",
            cancellationToken);

    public Task<TokenExchangeOutcome> RefreshAsync(
        RefreshLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateRefreshTokenGrant(),
            "Identity refresh token",
            cancellationToken);

    public Task<TokenExchangeOutcome> LoginWithDeviceAsync(
        DeviceLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateDeviceGrant(),
            "Identity login with device",
            cancellationToken);

    public Task<TokenExchangeOutcome> LoginWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(
            request.Context,
            request.CreateAuthorizationCodeGrant(),
            "Identity login with authorization code",
            cancellationToken);

    private async Task<TokenExchangeOutcome> SendTokenRequestAsync(
        BitwardenClientContext context,
        IReadOnlyDictionary<string, string> form,
        string operation,
        CancellationToken cancellationToken)
    {
        Uri tokenEndpoint = new(context.Environment.IdentityBase, "/connect/token");
        using FormUrlEncodedContent content = new(form);
        using HttpRequestMessage request = new(HttpMethod.Post, tokenEndpoint);
        request.Content = content;

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response is { IsSuccessStatusCode: false, StatusCode: HttpStatusCode.BadRequest })
        {
            var failureResponse = await response.Content.ReadFromJsonAsync<TokenFailureResponse>(
                BitwardenApiJsonContext.ConfiguredDefault.TokenFailureResponse,
                cancellationToken: cancellationToken);

            if (failureResponse is null)
                throw new InvalidDataException("Response JSON payload was empty.");

            return failureResponse.ToTokenFailureOutcome();
        }

        response.EnsureSuccess(operation, cancellationToken);

        TokenSuccessResponse? payload = await response.Content.ReadFromJsonAsync(
            BitwardenApiJsonContext.ConfiguredDefault.TokenSuccessResponse,
            cancellationToken);

        if (payload is null)
            throw new InvalidDataException("Response JSON payload was empty.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken.Value))
            throw new InvalidDataException("Identity token response did not include access_token.");

        return new TokenExchangeOutcome.Success(payload.ToTokenResponse());
    }
}
