using AsyncAwaitBestPractices;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcEventClient(string pipeName, ILogger<PipeIpcEventClient> logger)
    : BackgroundService, IIpcEventClient
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; narrowing would break resilience against unanticipated transport failures.")]
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
                    byte[] buffer = new byte[header.PayloadLength];
                    await pipe.ReadExactlyAsync(buffer, stoppingToken);

                    DispatchEvent(header.MessageType, buffer, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                //
            }
            catch (IOException)
            {
                logger.EventConnectionClosed();
            }
            catch (Exception exception)
            {
                logger.EventClientLoopFailed(exception);
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
            subscription.InvokeAsync(data, cancellationToken).SafeFireAndForget();
        }
    }

    internal void Unsubscribe(IIpcEventSubscription eventSubscription)
    {
        using var _ = _subscriptionsLock.EnterScope();
        _subscriptions.Remove(eventSubscription);
    }
}