using FluentBitwarden.BrowserHost.Dispatching;
using FluentBitwarden.BrowserHost.Ipc;
using FluentBitwarden.BrowserHost.NativeMessaging;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.BrowserHost;

internal static class Program
{
    private static async Task<int> Main()
    {
        var processCancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            processCancellation.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => processCancellation.Cancel();

        await using var stdin = Console.OpenStandardInput();
        await using var stdout = Console.OpenStandardOutput();
        using var serviceProvider = new ServiceCollection()
            .AddIpcClient(IpcConstants.AppHostPipeName)
            .BuildServiceProvider();

        var reader = new NativeMessageReader(stdin);
        var writer = new NativeMessageWriter(stdout);
        var appHostClient = new AppHostBrowserIpcClient(serviceProvider.GetRequiredService<IIpcClient>());
        var dispatcher = new BrowserNativeMessageDispatcher(appHostClient);

        return await RunAsync(reader, writer, dispatcher, processCancellation.Token);
    }

    private static async Task<int> RunAsync(
        NativeMessageReader reader,
        NativeMessageWriter writer,
        BrowserNativeMessageDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NativeMessageReadResult readResult;

            try
            {
                readResult = await reader.ReadAsync(cancellationToken);
            }
            catch (NativeMessageProtocolException exception)
            {
                await writer.WriteAsync(
                    BrowserResponseEnvelope.CreateError(null, exception.Code, exception.Message),
                    cancellationToken);

                if (!exception.CanContinue)
                    return 1;

                continue;
            }

            if (readResult.IsEndOfStream)
                return 0;

            BrowserResponseEnvelope response;

            try
            {
                var request = BrowserRequestEnvelope.Parse(readResult.Json);
                response = await dispatcher.DispatchAsync(request, cancellationToken);
            }
            catch (BrowserJsonException exception)
            {
                response = BrowserResponseEnvelope.CreateError(exception.Id, exception.Code, exception.Message);
            }

            await writer.WriteAsync(response, cancellationToken);
        }

        return 0;
    }
}
