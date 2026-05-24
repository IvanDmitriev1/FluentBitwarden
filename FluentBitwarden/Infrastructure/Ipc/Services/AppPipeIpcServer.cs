using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppPipeIpcServer : IIpcPipeServer, IAsyncDisposable
{
    public AppPipeIpcServer(IServiceProvider serviceProvider, IEnumerable<PipeMessageInvokerDescriptor> descriptors)
    {
        _serviceProvider = serviceProvider;
        _invokers = descriptors.ToDictionary(static desc => desc.MessageType, static desc => desc);
    }

    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<ushort, PipeMessageInvokerDescriptor> _invokers;

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
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
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

                var header = RequestHeader.Read(pipe);
                if (!_invokers.TryGetValue(header.MessageType, out var descriptor))
                    continue;

                var invoker = descriptor.CreateInvoker.Invoke(_serviceProvider);
                await invoker.InvokeAsync(pipe, header.PayloadLength, cancellationToken);
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
