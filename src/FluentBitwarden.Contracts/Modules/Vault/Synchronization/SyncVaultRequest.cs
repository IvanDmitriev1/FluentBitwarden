namespace FluentBitwarden.Contracts.Modules.Vault.Synchronization;

[MemoryPackable]
public readonly partial record struct SyncVaultRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.Sync;
}
