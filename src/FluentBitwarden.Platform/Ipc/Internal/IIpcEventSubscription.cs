namespace FluentBitwarden.Platform.Ipc.Internal;

internal interface IIpcEventSubscription : IDisposable
{
    ushort MessageType { get; }

    Task InvokeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}
