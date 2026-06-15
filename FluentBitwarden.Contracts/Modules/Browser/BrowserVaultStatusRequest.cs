namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial class BrowserVaultStatusRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetVaultStatus;
}
