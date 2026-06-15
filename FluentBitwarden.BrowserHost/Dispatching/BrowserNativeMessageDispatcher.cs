using FluentBitwarden.BrowserHost.Ipc;
using FluentBitwarden.Contracts.Modules.Browser;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace FluentBitwarden.BrowserHost.Dispatching;

internal sealed class BrowserNativeMessageDispatcher(AppHostBrowserIpcClient appHostClient)
{
    public async ValueTask<BrowserResponseEnvelope> DispatchAsync(
        BrowserRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Type))
        {
            return BrowserResponseEnvelope.CreateError(
                request.Id,
                "invalid_request",
                "Native message request must include a non-empty id and type.");
        }

        try
        {
            return request.Type switch
            {
                "status.get" => await GetStatusAsync(request.Id, cancellationToken),
                "credentials.availability" => await GetCredentialAvailabilityAsync(request, cancellationToken),
                "credentials.getForFill" => await GetCredentialFillAsync(request, cancellationToken),
                _ => BrowserResponseEnvelope.CreateError(
                    request.Id,
                    "unknown_message_type",
                    $"Unknown native message type '{request.Type}'.")
            };
        }
        catch (AppHostBrowserIpcException exception)
        {
            return BrowserResponseEnvelope.CreateError(request.Id, exception.Code, exception.Message);
        }
    }

    private async ValueTask<BrowserResponseEnvelope> GetStatusAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var response = await appHostClient.SendAsync<BrowserVaultStatusRequest, BrowserVaultStatusResponse>(
            new BrowserVaultStatusRequest(),
            cancellationToken);

        return BrowserResponseEnvelope.CreateSuccess(
            id,
            JsonSerializer.SerializeToElement(
                response,
                BrowserHostJsonContext.Default.BrowserVaultStatusResponse));
    }

    private async ValueTask<BrowserResponseEnvelope> GetCredentialAvailabilityAsync(
        BrowserRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!TryReadPayload(
                request,
                BrowserHostJsonContext.Default.BrowserCredentialAvailabilityPayload,
                out var payload,
                out var error))
        {
            return error;
        }

        if (string.IsNullOrWhiteSpace(payload.Origin) ||
            string.IsNullOrWhiteSpace(payload.Url) ||
            payload.HasPasswordField is null)
        {
            return BrowserResponseEnvelope.CreateError(
                request.Id,
                "invalid_payload",
                "Credential availability payload must include origin, url, and hasPasswordField.");
        }

        var response = await appHostClient.SendAsync<BrowserCredentialAvailabilityRequest, BrowserCredentialAvailabilityResponse>(
            new BrowserCredentialAvailabilityRequest(payload.Origin, payload.Url, payload.HasPasswordField.Value),
            cancellationToken);

        return BrowserResponseEnvelope.CreateSuccess(
            request.Id!,
            JsonSerializer.SerializeToElement(
                response,
                BrowserHostJsonContext.Default.BrowserCredentialAvailabilityResponse));
    }

    private async ValueTask<BrowserResponseEnvelope> GetCredentialFillAsync(
        BrowserRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!TryReadPayload(
                request,
                BrowserHostJsonContext.Default.BrowserCredentialFillPayload,
                out var payload,
                out var error))
        {
            return error;
        }

        if (string.IsNullOrWhiteSpace(payload.ItemId) ||
            string.IsNullOrWhiteSpace(payload.Origin) ||
            string.IsNullOrWhiteSpace(payload.Url) ||
            payload.UserGesture is null)
        {
            return BrowserResponseEnvelope.CreateError(
                request.Id,
                "invalid_payload",
                "Credential fill payload must include itemId, origin, url, and userGesture.");
        }

        var response = await appHostClient.SendAsync<BrowserCredentialFillRequest, BrowserCredentialFillResponse>(
            new BrowserCredentialFillRequest(payload.ItemId, payload.Origin, payload.Url, payload.UserGesture.Value),
            cancellationToken);

        return BrowserResponseEnvelope.CreateSuccess(
            request.Id!,
            JsonSerializer.SerializeToElement(
                response,
                BrowserHostJsonContext.Default.BrowserCredentialFillResponse));
    }

    private static bool TryReadPayload<T>(
        BrowserRequestEnvelope request,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        [NotNullWhen(true)] out T? payload,
        [NotNullWhen(false)] out BrowserResponseEnvelope? error)
    {
        if (request.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            payload = default;
            error = BrowserResponseEnvelope.CreateError(
                request.Id,
                "invalid_payload",
                "Native message payload is required.");

            return false;
        }

        try
        {
            payload = request.Payload.Deserialize(jsonTypeInfo);
            if (payload is not null)
            {
                error = null;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        payload = default;
        error = BrowserResponseEnvelope.CreateError(
            request.Id,
            "invalid_payload",
            "Native message payload is invalid.");

        return false;
    }
}
