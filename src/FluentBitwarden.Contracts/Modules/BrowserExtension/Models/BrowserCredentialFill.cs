namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialFillRequest(
    [property: StronglyTypedIdFormatter<CipherId>] CipherId ItemId,
    string Url,
    BrowserCredentialPart Part) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Browser.GetCredentialFill;
}

[MemoryPackable]
public partial class BrowserCredentialFillResponse
{
    public static readonly BrowserCredentialFillResponse Empty = new();

    public BrowserCredentialPart ReturnedParts { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Totp { get; init; }
    public DateTimeOffset? TotpExpiresAt { get; init; }
}
