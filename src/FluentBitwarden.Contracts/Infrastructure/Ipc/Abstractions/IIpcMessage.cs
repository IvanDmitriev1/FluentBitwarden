namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

public interface IIpcMessage
{
    static abstract ushort MessageType { get; }
}
