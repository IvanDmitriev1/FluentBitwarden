namespace FluentBitwarden.Contracts.Modules.Vault.Folders;

[MemoryPackable]
public readonly partial record struct GetVaultFoldersRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.GetFolders;
}
