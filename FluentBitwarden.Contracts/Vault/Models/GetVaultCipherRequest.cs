using BitwardenApi.Common.MemoryPackFormatters;
using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Vault.Models;

[MemoryPackable]
public readonly partial record struct GetVaultCipherRequest([property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.GetCipher;
}