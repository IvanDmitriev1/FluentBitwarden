using FluentBitwarden.BrowserHost.Infrastructure;
using FluentBitwarden.BrowserHost.Models;
using FluentBitwarden.BrowserHost.NativeMessaging;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.BrowserExtension;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentBitwarden.Contracts.Infrastructure.Shared;

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

    while (!cts.IsCancellationRequested)
    {
        var request = await native.ReadRequestAsync(cts.Token);
        if (request is null)
            return 0;

        await DispatchRequestAsync(native, browserExtensionClient, request, cts.Token);
    }

    return 0;
}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    return -1;
}
static async Task DispatchRequestAsync(
    INativeMessagingTransport messagingTransport,
    IBrowserExtensionClient client,
    BrowserNativeRequestEnvelope request,
    CancellationToken cancellationToken)
{
    try
    {
        switch (request.Type)
        {
            case IpcMessageTypes.Browser.GetVaultStatus:
                await HandleAsync(
                    messagingTransport,
                    client,
                    request,
                    BrowserHostJsonContext.Default.BrowserVaultStatusRequest,
                    BrowserHostJsonContext.Default.BrowserVaultStatusResponse,
                    static (client, payload, ct) => client.GetStatusAsync(payload, ct),
                    cancellationToken);
                return;

            case IpcMessageTypes.Browser.GetCredentialAvailability:
                await HandleAsync(
                    messagingTransport,
                    client,
                    request,
                    BrowserHostJsonContext.Default.BrowserCredentialAvailabilityRequest,
                    BrowserHostJsonContext.Default.BrowserCredentialAvailabilityResponse,
                    static (client, payload, ct) => client.CheckCredentialAvailabilityAsync(payload, ct),
                    cancellationToken);
                return;

            case IpcMessageTypes.Browser.GetCredentialFill:
                await HandleAsync(
                    messagingTransport,
                    client,
                    request,
                    BrowserHostJsonContext.Default.BrowserCredentialFillRequest,
                    BrowserHostJsonContext.Default.BrowserCredentialFillResponse,
                    static (client, payload, ct) => client.FillCredentialAsync(payload, ct),
                    cancellationToken);
                return;

            default:
                return;
        }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        UnhandledExceptionLogger.WriteException(ex);
    }
}

static async Task HandleAsync<TRequest, TResponse>(
    INativeMessagingTransport messagingTransport,
    IBrowserExtensionClient client,
    BrowserNativeRequestEnvelope request,
    JsonTypeInfo<TRequest> requestJsonTypeInfo,
    JsonTypeInfo<TResponse> responseJsonTypeInfo,
    Func<IBrowserExtensionClient, TRequest, CancellationToken, ValueTask<TResponse>> handler,
    CancellationToken cancellationToken)
{
    TRequest payload = request.Payload.Deserialize(requestJsonTypeInfo) ??
                       throw new JsonException($"Payload for browser request type {request.Type} was null or invalid.");

    TResponse response = await handler.Invoke(client, payload, cancellationToken);

    await messagingTransport.WriteResponseAsync(
        request.RequestId,
        response,
        responseJsonTypeInfo,
        cancellationToken);
}