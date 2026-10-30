namespace FluentBitwarden.Contracts.Modules.Vault.Ciphers;

/// <summary>
/// Creates the cipher when <see cref="Cipher.Id"/> is empty, otherwise updates it.
/// </summary>
[MemoryPackable]
public partial record SaveVaultCipherRequest(Cipher Cipher) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.SaveCipher;
}
