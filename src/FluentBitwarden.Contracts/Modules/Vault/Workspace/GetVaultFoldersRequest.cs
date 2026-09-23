namespace FluentBitwarden.Contracts.Modules.Vault.Workspace;

[MemoryPackable]
public readonly partial record struct GetVaultFoldersRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.GetFolders;
}
