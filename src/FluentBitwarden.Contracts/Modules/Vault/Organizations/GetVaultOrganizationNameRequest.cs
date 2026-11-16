namespace FluentBitwarden.Contracts.Modules.Vault.Organizations;

[MemoryPackable]
public readonly partial record struct GetVaultOrganizationNameRequest(OrganizationId OrganizationId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.GetOrganizationName;
}
