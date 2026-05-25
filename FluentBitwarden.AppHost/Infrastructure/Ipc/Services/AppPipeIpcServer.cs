using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;
using Microsoft.Extensions.Hosting;
using System.IO.Pipes;
using System.Linq;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppPipeIpcServer : BackgroundService
{
    public AppPipeIpcServer(string pipeName, IEnumerable<IPipeRequestHandlerInvoker> invokers)
    {
        _pipeName = pipeName;
        _invokers = invokers.ToDictionary(static i => i.MessageType);
    }

    private readonly string _pipeName;
    private readonly IReadOnlyDictionary<ushort, IPipeRequestHandlerInvoker> _invokers;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var pipe = new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
            inBufferSize: 64 * 1024,
            outBufferSize: 64 * 1024);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Debug.WriteLine("Waiting for named pipe client......");
                await pipe.WaitForConnectionAsync(stoppingToken);
                Debug.WriteLine("Named pipe client connected.");

                if (!PipeClientVerifier.IsExpectedClient(pipe))
                {
                    Debug.WriteLine("Rejected unauthorized IPC pipe client.");
                    continue;
                }

                var header = RequestHeader.Read(pipe);
                if (!_invokers.TryGetValue(header.MessageType, out var invoker))
                    return;

                await invoker.InvokeAsync(pipe, header.PayloadLength, stoppingToken);
            }
            catch (EndOfStreamException)
            {
                Debug.WriteLine("Client disconnected before the full IPC message was read.");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("IPC pipe server cancellation requested.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                if (pipe.IsConnected)
                    pipe.Disconnect();
            }
        }
    }
}
