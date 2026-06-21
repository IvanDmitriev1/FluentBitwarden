using FluentBitwarden.Contracts.Ipc.Abstractions;

namespace FluentBitwarden.Contracts.Modules.Ssh;

[MemoryPackable]
public sealed partial record SshUserActionRequest(
    string KeyName,
    string KeyFingerprint,
    bool IsForwarded) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Ui.ShowSshDialog;
}
