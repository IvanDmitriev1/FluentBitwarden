namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityRequest(
    string Url) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetCredentialAvailability;
}


[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityResponse(BrowserCredentialListItem[] Items)
{
    public static readonly BrowserCredentialAvailabilityResponse Empty = new([]);
}
