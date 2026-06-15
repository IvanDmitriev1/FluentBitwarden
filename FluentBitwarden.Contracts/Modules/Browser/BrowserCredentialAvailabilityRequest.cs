namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityRequest(
    string Origin,
    string Url,
    bool HasPasswordField) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetCredentialAvailability;
}
