using FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using Microsoft.Extensions.Hosting;
using System.IO.Pipes;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Services;

internal sealed class PipeIpcServer : BackgroundService
{ 
    public PipeIpcServer(string pipeName, IEnumerable<IIpcRequestHandlerInvoker> invokers)
    {
        _pipeName = pipeName;
        _invokers = invokers.ToDictionary(static i => i.MessageType);
    }

    private readonly string _pipeName;
    private readonly IReadOnlyDictionary<ushort, IIpcRequestHandlerInvoker> _invokers;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                inBufferSize: 4 * 1024,
                outBufferSize: 8 * 1024);

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

                var header = await RequestHeader.ReadAsync(pipe);
                if (!_invokers.TryGetValue(header.MessageType, out var invoker))
                    return;

                await invoker.InvokeAsync(pipe, header.PayloadLength, stoppingToken);
                await pipe.FlushAsync(stoppingToken);
                //pipe.c();
            }
            catch (EndOfStreamException)
            {
                Debug.WriteLine("Client disconnected before the full IPC message was read.");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("IPC pipe server cancellation requested.");
            }
            catch (IOException ioException) when (IsClientDisconnect(ioException))
            {
                Debug.WriteLine("IPC client disconnected before the server response was delivered.");
            }
            catch (Exception e)
            {
                UnhandledExceptionLogger.WriteException(e);
            }
        }
    }

    private static bool IsClientDisconnect(IOException exception) =>
        exception.HResult == PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_BROKEN_PIPE);
}
