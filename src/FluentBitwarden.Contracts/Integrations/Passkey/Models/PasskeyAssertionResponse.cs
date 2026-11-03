namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public sealed partial class PasskeyAssertionResponse
{
    public required byte[] CredentialId { get; init; }
    public required byte[] UserId { get; init; }
    public required byte[] AuthenticatorData { get; init; }
    public required byte[] Signature { get; init; }
    public required string UserName { get; init; }
    public required string UserDisplayName { get; init; }
}
