namespace FluentBitwarden.Contracts.Modules.Vault.Operations;

[MemoryPackable]
public readonly partial record struct SyncVaultRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.Sync;
}
