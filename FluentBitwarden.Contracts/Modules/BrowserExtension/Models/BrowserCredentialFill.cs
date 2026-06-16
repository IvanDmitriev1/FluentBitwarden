namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialFillRequest(
    string ItemId,
    string Url,
    bool UserGesture) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetCredentialFill;
}


[MemoryPackable]
public partial class BrowserCredentialFillResponse
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

