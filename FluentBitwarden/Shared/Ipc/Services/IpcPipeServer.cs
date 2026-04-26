using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Ipc.Internal;
using FluentBitwarden.Shared.Ipc.Models;
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

    private CancellationTokenSource _cts = new();
    private readonly Dictionary<ushort, IPipeMessageInvoker> _invokers;

    private readonly NamedPipeServerStream _pipe = new(
        IpcConstants.PipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
        inBufferSize: 64 * 1024,
        outBufferSize: 64 * 1024);

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();

        await _pipe.DisposeAsync();
    }

    public async Task RunAsync()
    {
        var cancellationToken = _cts.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            Debug.WriteLine("Waiting for named pipe client......");
            await _pipe.WaitForConnectionAsync(cancellationToken);
            Debug.WriteLine("Named pipe client connected.");

            try
            {
                var header = PipeHeader.Read(_pipe);
                if (!_invokers.TryGetValue(header.MessageType, out var invoker))
                    continue;

                await invoker.InvokeAsync(_pipe, header.PayloadLength, cancellationToken);
            }
            catch (EndOfStreamException)
            {
                Debug.WriteLine("Client disconnected before the full IPC message was read.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                if (_pipe.IsConnected)
                    _pipe.Disconnect();
            }
        }
    }
}
