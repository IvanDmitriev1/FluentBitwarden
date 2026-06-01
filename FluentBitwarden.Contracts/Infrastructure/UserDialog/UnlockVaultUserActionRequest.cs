using FluentBitwarden.Contracts.Modules;

namespace FluentBitwarden.Contracts.Infrastructure.UserDialog;

[MemoryPackable]
public sealed partial record UnlockVaultUserActionRequest(
    bool KeepOverlayOpenAfterUnlock = false) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Ui.ShowUnlockDialog;
}
