using FluentBitwarden.BrowseProxy.Infrastructure;
using FluentBitwarden.BrowseProxy.Models;
using FluentBitwarden.BrowseProxy.NativeMessaging;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.BrowserExtension;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

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
        var cancellationToken = cts.Token;

        try
        {
            var request = await native.ReadRequestAsync(cancellationToken);
            if (request is null)
                return 0;

            await DispatchRequestAsync(native, browserExtensionClient, request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //
        }
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
