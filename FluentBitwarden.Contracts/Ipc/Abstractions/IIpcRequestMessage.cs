namespace FluentBitwarden.Contracts.Ipc.Abstractions;

public interface IIpcRequestMessage
{
    static abstract ushort MessageType { get; }
}
