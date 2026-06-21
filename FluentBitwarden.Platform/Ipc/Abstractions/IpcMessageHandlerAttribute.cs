namespace FluentBitwarden.Platform.Ipc.Abstractions;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class IpcMessageHandlerAttribute : Attribute
{
    public IpcMessageHandlerAttribute() { }

    public IpcMessageHandlerAttribute(ushort messageType)
    {
        MessageType = messageType;
    }

    public ushort MessageType { get; set; }
    public IpcAuthenticationLevel AuthenticationLevel { get; set; } = IpcAuthenticationLevel.Authenticated;
}