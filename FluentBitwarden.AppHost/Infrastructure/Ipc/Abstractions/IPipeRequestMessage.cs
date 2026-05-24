namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeRequestMessage
{
    static abstract ushort MessageType { get; }
}
