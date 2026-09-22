namespace FluentBitwarden.Platform.Ipc;

public static class IpcConstants
{
    public const UInt16 ProtocolVersion = 3;
    public const string AppHostPipeName = @"LOCAL\FluentBitwarden.v3";
    public const string AppHostEventsPipeName = @"LOCAL\FluentBitwarden.Events.v3";
    public const string UiPipeName = @"LOCAL\FluentBitwarden.Ui.v3";
}
