using System.Net.Http.Json;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Serialization;
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

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ApiRevisionDateParser.Parse(body, response.Content.Headers.ContentType?.MediaType);
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
                    new AuthRequestCreateRequestDto
                    {
                        Email = email,
                        DeviceIdentifier = deviceIdentifier,
                        PublicKey = publicKey,
                        Type = (int)requestType,
                        AccessCode = accessCode,
                    },
                    BitwaredApiJsonContext.Default.AuthRequestCreateRequestDto),
            };

            request.Headers.TryAddWithoutValidation("Device-Identifier", deviceIdentifier);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            await response.EnsureBitwaredSuccessAsync("API endpoint", cancellationToken);

            try
            {
                AuthRequestCreateResponseDto? payload = await response.Content.ReadFromJsonAsync(
                    BitwaredApiJsonContext.Default.AuthRequestCreateResponseDto,
                    cancellationToken);

                return CreateAuthRequestResponse(
                    payload ?? throw new ServerVersionMismatchException("Auth request response was empty."),
                    accessCode);
            }
            catch (JsonException ex)
            {
                throw new ServerVersionMismatchException("Auth request response was not a supported JSON payload.", ex);
            }
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

            try
            {
                AuthRequestPollResponseDto? payload = await response.Content.ReadFromJsonAsync(
                    BitwaredApiJsonContext.Default.AuthRequestPollResponseDto,
                    cancellationToken);

                return CreateAuthRequestPollOutcome(
                    payload ?? throw new ServerVersionMismatchException("Auth request poll response was empty."));
            }
            catch (JsonException ex)
            {
                throw new ServerVersionMismatchException("Auth request poll response was not a supported JSON payload.", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    private static AuthRequestCreateResponse CreateAuthRequestResponse(
        AuthRequestCreateResponseDto dto,
        string accessCode)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new AuthRequestCreateResponse(
            dto.Id ?? throw new ServerVersionMismatchException("Auth request response did not include an Id."),
            accessCode,
            (dto.CreationDate ?? throw new ServerVersionMismatchException(
                "Auth request response did not include required property 'creationDate'."))
            .AddMinutes(15));
    }

    private static AuthRequestPollOutcome CreateAuthRequestPollOutcome(AuthRequestPollResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        bool answered = dto.RequestApproved is not null;
        bool approved = dto.RequestApproved == true;
        DateTimeOffset creationDate = dto.CreationDate ?? throw new ServerVersionMismatchException(
            "Auth request poll response did not include required property 'creationDate'.");

        if (creationDate.AddMinutes(15) <= nowUtc)
        {
            return new AuthRequestPollOutcome.Expired("The device login request expired before approval.");
        }

        if (!answered)
        {
            return new AuthRequestPollOutcome.Pending();
        }

        if (!approved || string.IsNullOrWhiteSpace(dto.Key))
        {
            return new AuthRequestPollOutcome.Denied("The device login request was denied.");
        }

        return new AuthRequestPollOutcome.Approved(
            new AuthRequestApproval(
                dto.Key,
                dto.ResponseDate,
                dto.RequestDeviceIdentifier,
                dto.RequestIpAddress,
                dto.RequestCountryName));
    }
}
