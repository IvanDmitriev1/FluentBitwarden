using FluentBitwarden.Platform.Ipc.Services;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal sealed class IpcEventSubscription<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
    AsyncIpcEventCallback<TEvent> callback,
    PipeIpcEventClient eventClient) : IIpcEventSubscription
    where TEvent : IIpcEventMessage
{
    public ushort MessageType => TEvent.MessageType;

    private bool _disposed;

    public Task InvokeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var message = MemoryPackSerializer.Deserialize<TEvent>(payload.Span);
        if (message is null)
        {
            throw new InvalidOperationException("Failed to deserialize IPC message");
        }

        return callback.Invoke(message, cancellationToken);
    }

    public void Dispose()
    {
        if (!Interlocked.Exchange(ref _disposed, true))
            return;

        eventClient.Unsubscribe(this);
    }
}
