using BitwardenApi.Infrastructure.Serialization;
using FluentBitwarden.Contracts.Ipc.Abstractions;

namespace FluentBitwarden.Contracts.Modules.Vault.Workspace;

[MemoryPackable]
public readonly partial record struct GetVaultCipherRequest([property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.GetCipher;
}