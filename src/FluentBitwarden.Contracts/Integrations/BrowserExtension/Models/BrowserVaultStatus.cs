namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial class BrowserVaultStatusRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetVaultStatus;
}


[MemoryPackable]
public sealed partial record BrowserVaultStatusResponse(
    bool IsRunning,
    bool IsVaultUnlocked);
