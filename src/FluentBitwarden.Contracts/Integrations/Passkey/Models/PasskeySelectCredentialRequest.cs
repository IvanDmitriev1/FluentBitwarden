namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public sealed partial record PasskeySelectCredentialRequest(string RpId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Ui.ShowPasskeySelectionDialog;
}
