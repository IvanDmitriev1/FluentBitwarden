namespace FluentBitwarden.Platform.Ipc.Internal;

internal interface IIpcEventWaiter : IDisposable
{ 
    ushort MessageType { get; }

    void Complete(ReadOnlySpan<byte> payload);
}