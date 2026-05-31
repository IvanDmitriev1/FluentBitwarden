namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class IpcMessageHandlerAttribute(ushort messageType) : Attribute
{
    public ushort MessageType { get; } = messageType;
}