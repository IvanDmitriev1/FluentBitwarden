using System.Net.Http.Json;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Utils;

namespace BitwaredApi.Services;

internal sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    public async ValueTask<HttpResponseMessage> CreateSyncResponseAsync(
        BitwardenEnvironment environment,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpRequestMessage request = new(HttpMethod.Get, environment.ApiBase.AppendRelativePath("/sync"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            try
            {
                await response.EnsureBitwaredSuccessAsync("API endpoint", cancellationToken);
                return response;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    public async ValueTask<DateTimeOffset?> GetRevisionDateAsync(
        BitwardenEnvironment environment,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpRequestMessage request = new(HttpMethod.Get, environment.ApiBase.AppendRelativePath("/accounts/revision-date"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            await response.EnsureBitwaredSuccessAsync("API endpoint", cancellationToken);

            string text = await response.Content.ReadAsStringAsync(cancellationToken);
            return DateTimeOffset.TryParse(text.Trim('"'), out DateTimeOffset parsed)
                ? parsed
                : null;
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    public async ValueTask<AuthRequestCreateResponse> CreateAuthRequestAsync(
        BitwardenEnvironment environment,
        string email,
        string deviceIdentifier,
        string publicKey,
        AuthRequestType requestType,
        string accessCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpRequestMessage request = new(HttpMethod.Post, environment.ApiBase.AppendRelativePath("/auth-requests/"))
            {
                Content = JsonContent.Create(
                    new
                    {
                        email,
                        deviceIdentifier,
                        publicKey,
                        type = (int)requestType,
                        accessCode,
                    },
                    options: JsonDefaults.SerializerOptions),
            };

            request.Headers.TryAddWithoutValidation("Device-Identifier", deviceIdentifier);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            await response.EnsureBitwaredSuccessAsync("API endpoint", cancellationToken);

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            return ApiAuthRequestResponseParser.ParseCreateResponse(
                document.RootElement,
                accessCode,
                DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    public async ValueTask<AuthRequestPollOutcome> GetAuthResponseAsync(
        BitwardenEnvironment environment,
        string requestId,
        string accessCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpRequestMessage request = new(
                HttpMethod.Get,
                environment.ApiBase.AppendRelativePath($"/auth-requests/{requestId}/response?code={Uri.EscapeDataString(accessCode)}"));

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new AuthRequestPollOutcome.Expired("The auth request no longer exists on the server.");
            }

            await response.EnsureBitwaredSuccessAsync("API endpoint", cancellationToken);

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            return ApiAuthRequestResponseParser.ParsePollOutcome(document.RootElement, DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }
}
