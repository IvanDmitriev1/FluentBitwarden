namespace FluentBitwarden.Platform.Ipc.Abstractions;

public interface IIpcEventPublisher
{
    Task PublishAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        TEvent data,
        CancellationToken cancellationToken = default)
        where TEvent : IIpcEventMessage;
}
