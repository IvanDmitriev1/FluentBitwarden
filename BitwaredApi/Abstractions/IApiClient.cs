using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

internal interface IApiClient
{
    ValueTask<HttpResponseMessage> CreateSyncResponseAsync(
        BitwardenEnvironment environment,
        string accessToken,
        CancellationToken cancellationToken = default);

    ValueTask<DateTimeOffset?> GetRevisionDateAsync(
        BitwardenEnvironment environment,
        string accessToken,
        CancellationToken cancellationToken = default);

    ValueTask<AuthRequestCreateResponse> CreateAuthRequestAsync(
        BitwardenEnvironment environment,
        string email,
        string deviceIdentifier,
        string publicKey,
        AuthRequestType requestType,
        string accessCode,
        CancellationToken cancellationToken = default);

    ValueTask<AuthRequestPollOutcome> GetAuthResponseAsync(
        BitwardenEnvironment environment,
        string requestId,
        string accessCode,
        CancellationToken cancellationToken = default);
}
