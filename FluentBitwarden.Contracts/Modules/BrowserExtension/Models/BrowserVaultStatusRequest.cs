namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial class BrowserVaultStatusRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetVaultStatus;
}
