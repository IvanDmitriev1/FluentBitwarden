using System.Net.Http.Json;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Utilities;

namespace BitwaredApi.Services;

internal sealed class ApiClient(HttpClient httpClient, IEnvironmentConfig environmentConfig) : IApiClient
{
    public async ValueTask<JsonDocument> GetSyncAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(BuildUri("/sync"), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<DateTimeOffset?> GetRevisionDateAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(BuildUri("/accounts/revision-date"), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return DateTimeOffset.TryParse(text.Trim('"'), out DateTimeOffset parsed)
            ? parsed
            : null;
    }

    public async ValueTask<AuthRequestCreateResponse> CreateAuthRequestAsync(
        string email,
        string deviceIdentifier,
        string publicKey,
        AuthRequestType requestType,
        string accessCode,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(HttpMethod.Post, BuildUri("/auth-requests/"))
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

        request.Options.Set(HttpRequestOptionKeys.SkipAuthorization, true);
        request.Headers.TryAddWithoutValidation("Device-Identifier", deviceIdentifier);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        using JsonDocument document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        JsonElement root = document.RootElement;
        DateTimeOffset created = root.TryGetProperty("creationDate", out JsonElement creationDate)
            && DateTimeOffset.TryParse(creationDate.GetString(), out DateTimeOffset parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        return new AuthRequestCreateResponse(
            root.GetProperty("id").GetString() ?? throw new ServerVersionMismatchException("Auth request response did not include an Id."),
            accessCode,
            created.AddMinutes(15));
    }

    public async ValueTask<AuthRequestStatusResponse> GetAuthResponseAsync(
        string requestId,
        string accessCode,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(HttpMethod.Get, BuildUri($"/auth-requests/{requestId}/response?code={Uri.EscapeDataString(accessCode)}"));
        request.Options.Set(HttpRequestOptionKeys.SkipAuthorization, true);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new DeviceApprovalPendingException("The auth request no longer exists on the server.");
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        using JsonDocument document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        JsonElement root = document.RootElement;
        bool answered = root.TryGetProperty("requestApproved", out JsonElement approvedProp) && approvedProp.ValueKind != JsonValueKind.Null;
        bool approved = answered && approvedProp.GetBoolean();
        DateTimeOffset? responseDate = root.TryGetProperty("responseDate", out JsonElement responseDateProp)
            && DateTimeOffset.TryParse(responseDateProp.GetString(), out DateTimeOffset parsedResponseDate)
            ? parsedResponseDate
            : null;
        DateTimeOffset creationDate = root.TryGetProperty("creationDate", out JsonElement creationDateProp)
            && DateTimeOffset.TryParse(creationDateProp.GetString(), out DateTimeOffset parsedCreationDate)
            ? parsedCreationDate
            : DateTimeOffset.UtcNow;

        bool expired = creationDate.AddMinutes(15) <= DateTimeOffset.UtcNow;

        return new AuthRequestStatusResponse(
            approved,
            answered,
            expired,
            root.TryGetProperty("key", out JsonElement keyProp) ? keyProp.GetString() : null,
            responseDate,
            root.TryGetProperty("requestDeviceIdentifier", out JsonElement deviceProp) ? deviceProp.GetString() : null,
            root.TryGetProperty("requestIpAddress", out JsonElement ipProp) ? ipProp.GetString() : null,
            root.TryGetProperty("requestCountryName", out JsonElement countryProp) ? countryProp.GetString() : null);
    }

    private static async ValueTask EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new ServerVersionMismatchException($"API endpoint returned {(int)response.StatusCode}: {body}");
    }

    private Uri BuildUri(string relativePath)
        => new(environmentConfig.Current.ApiBase, relativePath);
}
