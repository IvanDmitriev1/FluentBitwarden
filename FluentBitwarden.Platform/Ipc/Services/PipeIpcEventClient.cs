using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;
using System.IO.Pipes;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcEventClient(string pipeName) : BackgroundService, IIpcEventClient
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromMilliseconds(250);

    private readonly Lock _waitersLock = new();
    private readonly List<IIpcEventWaiter> _waiters = [];

    private readonly Lock _subscriptionsLock = new();
    private readonly List<IIpcEventSubscription> _subscriptions = [];

    public IDisposable Subscribe<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        AsyncIpcEventCallback<TEvent> callback)
        where TEvent : IIpcEventMessage
    {
        using var _ = _subscriptionsLock.EnterScope();

        var subscription = new IpcEventSubscription<TEvent>(callback, this);
        _subscriptions.Add(subscription);

        return subscription;
    }

    public IDisposable Subscribe<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        IpcEventCallback<TEvent> handler)
        where TEvent : IIpcEventMessage => Subscribe<TEvent>((@event, _) =>
    {
        handler(@event);
        return Task.CompletedTask;
    });

    public async Task<TEvent> WaitAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        CancellationToken cancellationToken = default)
        where TEvent : IIpcEventMessage
    {
        using var waiter = new IpcEventWaiter<TEvent>(cancellationToken);

        lock (_waitersLock)
        {
            _waiters.Add(waiter);
        }

        try
        {
            return await waiter.Task;
        }
        finally
        {
            lock (_waitersLock)
            {
                _waiters.Remove(waiter);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.In,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            try
            {
                await pipe.ConnectAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var header = await IpcMessageHeader.ReadAsync(pipe, stoppingToken);
                    using var bufferOwner = MemoryOwner<byte>.Allocate(header.PayloadLength);
                    await pipe.ReadExactlyAsync(bufferOwner.Memory, stoppingToken);

                    DispatchEvent(header.MessageType, bufferOwner.Memory, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                //
            }
            catch (IOException)
            {
                Debug.WriteLine("IPC event connection closed; reconnecting.");
            }
            catch (Exception exception)
            {
                UnhandledExceptionLogger.WriteException(exception);
            }

            await Task.Delay(ReconnectDelay, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private void DispatchEvent(ushort messageType, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        IIpcEventWaiter[] waiters;
        IIpcEventSubscription[] subscriptions;

        lock (_waiters)
        {
            waiters = _waiters.Where(w => w.MessageType == messageType).ToArray();
        }

        lock (_subscriptions)
        {
            subscriptions = _subscriptions.Where(s => s.MessageType == messageType).ToArray();
        }

        foreach (var waiter in waiters)
        {
            waiter.Complete(data.Span);
        }

        foreach (var subscription in subscriptions)
        {
            _ = subscription.InvokeAsync(data, cancellationToken);
        }
    }

    internal void Unsubscribe(IIpcEventSubscription eventSubscription)
    {
        using var _ = _subscriptionsLock.EnterScope();
        _subscriptions.Remove(eventSubscription);
    }
}
