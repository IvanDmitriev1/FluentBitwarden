namespace FluentBitwarden.Platform.Ipc.Internal;

internal abstract class IpcEventWaiter : IDisposable
{
    public abstract ushort MessageType { get; }

    
    public abstract void Complete(ReadOnlySpan<byte> payload);
    public abstract void TrySetCanceled();
    public abstract void TrySetException(Exception exception);
    public abstract void Dispose();
}

internal sealed class IpcEventWaiter<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>
    : IpcEventWaiter
    where TEvent : IIpcEventMessage
{
    private readonly TaskCompletionSource<TEvent> _cts = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenRegistration _cr;

    public IpcEventWaiter(CancellationToken cancellationToken)
    {
        _cr = cancellationToken.Register(
            () => _cts.TrySetCanceled(cancellationToken));
    }

    public override ushort MessageType => TEvent.MessageType;
    public Task<TEvent> Task => _cts.Task;

    public override void Complete(ReadOnlySpan<byte> payload)
    {
        var message = MemoryPackSerializer.Deserialize<TEvent>(payload);
        if (message is not null)
        {
            _cts.TrySetResult(message);
            return;
        }

        _cts.TrySetException(new InvalidOperationException("Failed to deserialize IPC message"));
    }

    public override void TrySetCanceled() => _cts.TrySetCanceled();

    public override void TrySetException(Exception exception) => _cts.TrySetException(exception);
    public override void Dispose()
    {
        _cts.TrySetCanceled();
        _cr.Dispose();
    }
}
