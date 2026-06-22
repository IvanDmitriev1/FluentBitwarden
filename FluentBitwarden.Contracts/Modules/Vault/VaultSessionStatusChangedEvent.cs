using FluentBitwarden.Contracts.Modules;

namespace FluentBitwarden.Contracts.Modules.Vault;

public enum VaultSessionStatus
{
    Locked,
    Unlocked,
}

[MemoryPackable]
public readonly partial record struct VaultSessionStatusChangedEvent(VaultSessionStatus Status) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.SessionStatusChanged;
}
