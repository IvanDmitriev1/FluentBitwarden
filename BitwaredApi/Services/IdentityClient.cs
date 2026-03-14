using System.Net;
using System.Net.Http.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Serialization;
using BitwaredApi.Utils;

namespace BitwaredApi.Services;

internal sealed class IdentityClient(HttpClient httpClient) : IIdentityClient
{
    public async ValueTask<PreloginResponseModel> PreloginAsync(
        BitwardenEnvironment environment,
        string email,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            environment.IdentityBase.AppendRelativePath("/accounts/prelogin"),
            new PreloginRequestDto
            {
                Email = email,
            },
            BitwaredApiJsonContext.Default.PreloginRequestDto,
            cancellationToken);

        await response.EnsureBitwaredSuccessAsync("Identity endpoint", cancellationToken);

        PreloginResponseDto? payload = await response.Content.ReadFromJsonAsync(
            BitwaredApiJsonContext.Default.PreloginResponseDto,
            cancellationToken);

        return (payload ?? throw new ServerVersionMismatchException("Identity prelogin response was empty.")).ToPreloginResponse();
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
        using var form = new FormUrlEncodedContent(request.ToFormValues());

        using HttpResponseMessage response = await httpClient.PostAsync(
            environment.IdentityBase.AppendRelativePath("/connect/token"),
            form,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ReadTokenFailure(body, response.StatusCode);
        }

        TokenSuccessResponseDto? payload = await response.Content.ReadFromJsonAsync(
            BitwaredApiJsonContext.Default.TokenSuccessResponseDto,
            cancellationToken);

        return new TokenExchangeOutcome.Success(
            (payload ?? throw new ServerVersionMismatchException("Identity token response was empty.")).ToTokenResponse(DateTimeOffset.UtcNow));
    }

    private static TokenExchangeOutcome ReadTokenFailure(string body, HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body))
        {
            try
            {
                TokenFailureResponseDto? payload = System.Text.Json.JsonSerializer.Deserialize(
                    body,
                    BitwaredApiJsonContext.Default.TokenFailureResponseDto);

                if (payload is not null)
                {
                    return payload.ToTokenFailureOutcome();
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new ServerVersionMismatchException(
                    $"Token endpoint returned {(int)statusCode}: {body}",
                    ex);
            }
        }

        throw new ServerVersionMismatchException($"Token endpoint returned {(int)statusCode}: {body}");
    }
}
