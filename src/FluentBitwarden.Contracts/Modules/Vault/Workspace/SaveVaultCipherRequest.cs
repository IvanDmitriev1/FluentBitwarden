namespace FluentBitwarden.Contracts.Modules.Vault.Workspace;

/// <summary>
/// Creates the cipher when <see cref="VaultCipher.Id"/> is empty, otherwise updates it.
/// </summary>
[MemoryPackable]
public partial record SaveVaultCipherRequest(VaultCipher Cipher) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.SaveCipher;
}
