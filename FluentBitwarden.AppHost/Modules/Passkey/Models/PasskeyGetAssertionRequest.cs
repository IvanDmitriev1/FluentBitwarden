using FluentBitwarden.Contracts.Ipc.Abstractions;
using MemoryPack;

namespace FluentBitwarden.Modules.Passkey.Models;

[MemoryPackable]
public readonly partial record struct PasskeyGetAssertionRequest(
    string RpId,
    byte[] RpIdHash,
    byte[] ClientDataHash) : IIpcRequestMessage
{
    public static ushort MessageType => 2;
}
