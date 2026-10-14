namespace FluentBitwarden.Contracts.Modules.Vault.Ciphers;

[MemoryPackable]
public readonly partial record struct GetVaultCipherRequest([property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.GetCipher;
}
