using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using MemoryPack;

namespace FluentBitwarden.Modules.Passkey.Models;

[MemoryPackable]
public readonly partial record struct PasskeyGetAssertionRequest(
    string RpId,
    byte[] RpIdHash,
    byte[] ClientDataHash) : IPipeRequestMessage
{
    public static ushort MessageType => 2;
}
