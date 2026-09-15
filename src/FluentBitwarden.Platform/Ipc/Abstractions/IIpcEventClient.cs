namespace FluentBitwarden.Platform.Ipc.Abstractions;

public interface IIpcEventClient
{
    IDisposable Subscribe<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        AsyncIpcEventCallback<TEvent> callback)
        where TEvent : IIpcEventMessage;

    IDisposable Subscribe<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        IpcEventCallback<TEvent> handler)
        where TEvent : IIpcEventMessage;

    Task<TEvent> WaitAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        CancellationToken cancellationToken = default)
        where TEvent : IIpcEventMessage;
}
