using System.IO.Pipes;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcEventHub(string pipeName, IIpcClientsVerifier ipcClientsVerifier)
    : BackgroundService, IIpcEventPublisher
{
    private sealed record Subscriber(NamedPipeServerStream Pipe) : IDisposable
    {
        public void Dispose() => Pipe.Dispose();
    }

    private readonly Lock _subscribersLock = new();
    private readonly List<Subscriber> _subscribers = [];
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    private NamedPipeServerStream CreatePipe() => new(
        pipeName,
        PipeDirection.Out,
        maxNumberOfServerInstances: NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
        inBufferSize: 4 * 1024,
        outBufferSize: 8 * 1024);

    public async Task PublishAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        TEvent data,
        CancellationToken cancellationToken = default)
        where TEvent : IIpcEventMessage
    {
        await _publishLock.WaitAsync(cancellationToken);
        
        try
        {
            Subscriber[] subscribers;
            lock (_subscribersLock)
            {
                subscribers = _subscribers.ToArray();
            }

            await Task.WhenAll(subscribers
                .Select(subscriber => TryWriteAsync(subscriber, data, cancellationToken)));
        }
        finally
        {
            _publishLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = CreatePipe();
                await pipe.WaitForConnectionAsync(stoppingToken);

                if (ipcClientsVerifier.IsExpectedClient(pipe) != IpcAuthenticationLevel.SamePackage)
                {
                    Debug.WriteLine("Rejected unauthorized IPC event subscriber.");
                    continue;
                }

                lock (_subscribersLock)
                {
                    _subscribers.Add(new Subscriber(pipe));
                    pipe = null;
                }

                Debug.WriteLine("IPC event subscriber registered.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                UnhandledExceptionLogger.WriteException(exception);
            }
            finally
            {
                pipe?.Dispose();
            }
        }
    }

    private async Task TryWriteAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Subscriber subscriber,
        TEvent message,
        CancellationToken cancellationToken)
        where TEvent : IIpcEventMessage
    {
        try
        {
            await IpcWireProtocol.WriteEventAsync(
                subscriber.Pipe,
                message,
                cancellationToken);

            await subscriber.Pipe.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //
        }
        catch (Exception exception) when (
            exception is IOException or ObjectDisposedException or InvalidOperationException)
        {

            lock (_subscribersLock)
            {
                _subscribers.Remove(subscriber);
            }

            subscriber.Dispose();
        }
    }
}
