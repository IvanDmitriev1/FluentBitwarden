using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Platform.Infrastructure.Extensions;
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
            try
            {
                await ReadEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (
                exception is EndOfStreamException ||
                (exception is IOException ioException && ioException.IsNamedPipeClientDisconnect()))
            {
                Debug.WriteLine("IPC event connection closed; reconnecting.");
            }
            catch (Exception exception)
            {
                UnhandledExceptionLogger.WriteException(exception);
            }

            await Task.Delay(ReconnectDelay, stoppingToken);
        }
    }

    private async Task ReadEventsAsync(CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.In,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        await pipe.ConnectAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            var header = await IpcMessageHeader.ReadAsync(pipe, cancellationToken);

            using var bufferOwner = MemoryOwner<byte>.Allocate(header.PayloadLength);
            await pipe.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

            IIpcEventWaiter[] waiters;
            IIpcEventSubscription[] subscriptions;

            lock (_waiters)
            {
                waiters = _waiters.Where(w => w.MessageType == header.MessageType).ToArray();
            }

            lock (_subscriptions)
            {
                subscriptions = _subscriptions.Where(s => s.MessageType == header.MessageType).ToArray();
            }

            foreach (var waiter in waiters)
            {
                waiter.Complete(bufferOwner.Span);
            }

            foreach (var subscription in subscriptions)
            {
                _ = subscription.InvokeAsync(bufferOwner.Memory, cancellationToken);
            }
        }
    }

    internal void Unsubscribe(IIpcEventSubscription eventSubscription)
    {
        using var _ = _subscriptionsLock.EnterScope();
        _subscriptions.Remove(eventSubscription);
    }
}
