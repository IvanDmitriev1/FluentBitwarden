namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

public interface IIpcRequestMessage
{
    static abstract ushort MessageType { get; }
}
