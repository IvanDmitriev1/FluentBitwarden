namespace FluentBitwarden.Contracts.AppSession.Unlock;

[MemoryPackable]
public sealed partial record UnlockAccountUserActionRequest(
    bool KeepOverlayOpenAfterUnlock = false) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Ui.ShowUnlockDialog;
}
