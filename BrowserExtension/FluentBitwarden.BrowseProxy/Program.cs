using FluentBitwarden.BrowseProxy.NativeMessaging;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Contracts.Modules;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

const int nativeProtocolVersion = 1;
TimeSpan requestTimeout = TimeSpan.FromSeconds(9);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await using var stdin = Console.OpenStandardInput();
    await using var stdout = Console.OpenStandardOutput();

    await using var services = new ServiceCollection()
        .AddIpcClient(IpcConstants.AppHostPipeName)
        .AddSingleton<IBrowserExtensionClient, RemoteBrowserExtensionClient>()
        .BuildServiceProvider();

    var native = new NativeMessagingTransport(stdin, stdout);
    var browserExtensionClient = services.GetRequiredService<IBrowserExtensionClient>();

    while (true)
    {
        var cancellationToken = cts.Token;
        BrowserNativeRequestEnvelope? request;

        try
        {
            request = await native.ReadRequestAsync(cancellationToken);
        }
        catch (JsonException e)
        {
            await Console.Error.WriteLineAsync($"Rejected malformed native message: {e}");
            continue;
        }

        if (request is null)
            return 0;

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestCts.CancelAfter(requestTimeout);

        try
        {
            ValidateRequest(request);
            await DispatchRequestAsync(native, browserExtensionClient, request, requestCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
        {
            await Console.Error.WriteLineAsync($"Native request '{request.RequestId}' timed out after {requestTimeout.TotalSeconds} seconds.");
        }
#pragma warning disable CA1031 // Intentional log-and-continue boundary; a single malformed native request must not crash the proxy process.
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Native request '{request.RequestId}' failed: {e}");
        }
#pragma warning restore CA1031
    }

}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    return 0;
}
#pragma warning disable CA1031 // Top-level catch-all; any unhandled failure must be logged and result in a non-zero exit code, not crash silently.
catch (Exception e)
{
    await Console.Error.WriteLineAsync(e.ToString());
    return -1;
#pragma warning restore CA1031
}

static void ValidateRequest(BrowserNativeRequestEnvelope request)
{
    if (request.Version != nativeProtocolVersion)
    {
        throw new InvalidDataException(
            $"Unsupported native protocol version {request.Version}; expected {nativeProtocolVersion}.");
    }

    if (string.IsNullOrWhiteSpace(request.RequestId))
        throw new InvalidDataException("Native request ID is required.");

    if (request.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        throw new InvalidDataException("Native request payload is required.");
}

static Task DispatchRequestAsync(
    INativeMessagingTransport messagingTransport,
    IBrowserExtensionClient client,
    BrowserNativeRequestEnvelope request,
    CancellationToken cancellationToken)
{
    Task task = request.Type switch
    {

        IpcMessageTypes.Browser.GetVaultStatus =>
            HandleAsync<BrowserVaultStatusRequest, BrowserVaultStatusResponse>(
                messagingTransport,
                client,
                request,
                static (client, payload, ct) => client.GetStatusAsync(payload, ct),
                cancellationToken),

        IpcMessageTypes.Browser.GetCredentialAvailability =>
            HandleAsync<BrowserCredentialAvailabilityRequest, BrowserCredentialAvailabilityResponse>(
                messagingTransport,
                client,
                request,
                static (client, payload, ct) => client.CheckCredentialAvailabilityAsync(payload, ct),
                cancellationToken),


        IpcMessageTypes.Browser.GetCredentialFill =>
            HandleAsync<BrowserCredentialFillRequest, BrowserCredentialFillResponse>(
                messagingTransport,
                client,
                request,
                static (client, payload, ct) => client.FillCredentialAsync(payload, ct),
                cancellationToken),
        _ => throw new InvalidOperationException($"Unsupported request type: {request.Type}")
    };

    return task;
}

static async Task HandleAsync<TRequest, TResponse>(
    INativeMessagingTransport messagingTransport,
    IBrowserExtensionClient client,
    BrowserNativeRequestEnvelope request,
    Func<IBrowserExtensionClient, TRequest, CancellationToken, ValueTask<TResponse>> handler,
    CancellationToken cancellationToken)
{
    var requestJsonTypeInfo = BrowseProxyJsonContext.ConfiguredDefault.GetRequiredTypeInfo<TRequest>();
    TRequest payload = request.Payload.Deserialize(requestJsonTypeInfo) ??
                       throw new JsonException($"Payload for browser request type {request.Type} was null or invalid.");

    TResponse response = await handler.Invoke(client, payload, cancellationToken);

    await messagingTransport.WriteResponseAsync(
        request.RequestId,
        response,
        cancellationToken);
}
