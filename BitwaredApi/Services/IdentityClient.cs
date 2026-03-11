using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Utils;

namespace BitwaredApi.Services;

internal sealed class IdentityClient(HttpClient httpClient) : IIdentityClient
{
    public async ValueTask<PreloginResponseModel> PreloginAsync(
        BitwardenEnvironment environment,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                environment.IdentityBase.AppendRelativePath("/accounts/prelogin"),
                new { email },
                JsonDefaults.SerializerOptions,
                cancellationToken);

            await response.EnsureBitwaredSuccessAsync("Identity endpoint", cancellationToken);

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            return IdentityTokenResponseParser.ParsePreloginResponse(document.RootElement);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    public ValueTask<TokenExchangeOutcome> ExchangePasswordAsync(
        BitwardenEnvironment environment,
        PasswordTokenRequestModel request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(environment, request, cancellationToken);

    public ValueTask<TokenExchangeOutcome> RefreshTokenAsync(
        BitwardenEnvironment environment,
        RefreshTokenRequestModel request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(environment, request, cancellationToken);

    private async ValueTask<TokenExchangeOutcome> SendTokenRequestAsync(
        BitwardenEnvironment environment,
        TokenRequestModel request,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent form = new(request.ToFormValues());

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(
                environment.IdentityBase.AppendRelativePath("/connect/token"),
                form,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await IdentityTokenResponseParser.ReadTokenFailureAsync(response, cancellationToken);
            }

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            return new TokenExchangeOutcome.Success(
                IdentityTokenResponseParser.ParseTokenSuccessResponse(
                    document.RootElement,
                    DateTimeOffset.UtcNow));
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }
}
