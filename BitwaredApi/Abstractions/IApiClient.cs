using System.Text.Json;
using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

public interface IApiClient
{
    ValueTask<JsonDocument> GetSyncAsync(CancellationToken cancellationToken = default);

    ValueTask<DateTimeOffset?> GetRevisionDateAsync(CancellationToken cancellationToken = default);

    ValueTask<AuthRequestCreateResponse> CreateAuthRequestAsync(
        string email,
        string deviceIdentifier,
        string publicKey,
        AuthRequestType requestType,
        string accessCode,
        CancellationToken cancellationToken = default);

    ValueTask<AuthRequestStatusResponse> GetAuthResponseAsync(
        string requestId,
        string accessCode,
        CancellationToken cancellationToken = default);
}
