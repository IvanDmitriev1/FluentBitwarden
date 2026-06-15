namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial record BrowserCredentialFillRequest(
    string ItemId,
    string Origin,
    string Url,
    bool UserGesture) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetCredentialFill;
}
