namespace FluentBitwarden.Platform.Ipc.Abstractions;

public delegate Task AsyncIpcEventCallback<in TEvent>(
    TEvent @event,
    CancellationToken cancellationToken)
    where TEvent : IIpcEventMessage;

public delegate void IpcEventCallback<in TEvent>(TEvent @event)
    where TEvent : IIpcEventMessage;