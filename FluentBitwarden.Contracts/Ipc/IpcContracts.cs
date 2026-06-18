namespace FluentBitwarden.Contracts.Ipc;

public static class IpcConstants
{
    public const UInt16 ProtocolVersion = 2;
    public const string AppHostPipeName = @"LOCAL\FluentBitwarden.v2";
    public const string UiPipeName = @"LOCAL\FluentBitwarden.Ui.v2";
}
