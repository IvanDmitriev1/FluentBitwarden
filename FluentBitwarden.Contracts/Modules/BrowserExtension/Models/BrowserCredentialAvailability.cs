namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityRequest(
    string Url,
    bool HasPasswordField) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetCredentialAvailability;
}


[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityResponse(int Count, BrowserCredentialListItem[] Items);
