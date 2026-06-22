namespace FluentBitwarden.Platform.Ipc.Abstractions;

public interface IIpcEventClient
{
    Task<TEvent> WaitAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        CancellationToken cancellationToken = default)
        where TEvent : IIpcEventMessage;
}
