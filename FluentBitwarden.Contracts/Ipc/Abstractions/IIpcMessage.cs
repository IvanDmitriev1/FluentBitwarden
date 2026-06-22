namespace FluentBitwarden.Contracts.Ipc.Abstractions;

public interface IIpcMessage
{
    static abstract ushort MessageType { get; }
}
