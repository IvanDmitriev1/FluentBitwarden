namespace FluentBitwarden.Platform.Ipc.Internal;

internal sealed class IpcEventWaiter<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>
    : IIpcEventWaiter
    where TEvent : IIpcEventMessage
{
    private readonly TaskCompletionSource<TEvent> _cts = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenRegistration _cr;
    private bool _disposed;

    public IpcEventWaiter(CancellationToken cancellationToken)
    {
        _cr = cancellationToken.Register(
            () => _cts.TrySetCanceled(cancellationToken));
    }

    public ushort MessageType => TEvent.MessageType;
    public Task<TEvent> Task => _cts.Task;

    public void Complete(ReadOnlySpan<byte> payload)
    {
        var message = MemoryPackSerializer.Deserialize<TEvent>(payload);
        if (message is not null)
        {
            _cts.TrySetResult(message);
            return;
        }

        _cts.TrySetException(new InvalidOperationException("Failed to deserialize IPC message"));
    }

    public void Dispose()
    {
        if (!Interlocked.Exchange(ref _disposed, true))
            return;

        _cts.TrySetCanceled();
        _cr.Dispose();
    }
}
