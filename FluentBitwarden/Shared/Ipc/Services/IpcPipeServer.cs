using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Ipc.Internal;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;

namespace FluentBitwarden.Shared.Ipc.Services;

[Fody.ConfigureAwait(false)]
internal sealed class IpcPipeServer : IIpcPipeServer, IAsyncDisposable
{
    public IpcPipeServer(IEnumerable<IPipeMessageInvoker> invokers)
    {
        _invokers = invokers.ToDictionary(static invoker => invoker.MessageType);
    }

    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<ushort, IPipeMessageInvoker> _invokers;

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }

    public async Task RunAsync()
    {
        var cancellationToken = _cts.Token;
        await using var pipe = new NamedPipeServerStream(
            IpcConstants.PipeName, PipeDirection.InOut,
            maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
            inBufferSize: 64 * 1024,
            outBufferSize: 64 * 1024);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Debug.WriteLine("Waiting for named pipe client......");
                await pipe.WaitForConnectionAsync(cancellationToken);
                Debug.WriteLine("Named pipe client connected.");

                if (!PipeClientVerifier.IsExpectedClient(pipe))
                {
                    Debug.WriteLine("Rejected unauthorized IPC pipe client.");
                    continue;
                }

                var header = PipeHeader.Read(pipe);
                if (!_invokers.TryGetValue(header.MessageType, out var invoker))
                    continue;

                await invoker.InvokeAsync(pipe, header.PayloadLength, cancellationToken);
            }
            catch (EndOfStreamException)
            {
                Debug.WriteLine("Client disconnected before the full IPC message was read.");
            }
            catch (OperationCanceledException)
            {
                //
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
