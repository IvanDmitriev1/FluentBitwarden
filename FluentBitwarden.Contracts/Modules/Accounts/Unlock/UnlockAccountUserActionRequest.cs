using FluentBitwarden.Contracts.Ipc.Abstractions;

namespace FluentBitwarden.Contracts.Modules.Accounts.Unlock;

[MemoryPackable]
public sealed partial record UnlockAccountUserActionRequest(
    bool KeepOverlayOpenAfterUnlock = false) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Ui.ShowUnlockDialog;
}
