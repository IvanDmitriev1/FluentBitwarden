namespace FluentBitwarden.Shared.Ipc;

public static class IpcConstants
{
    public const UInt16 ProtocolVersion = 1;
    public const string PipeName = @"LOCAL\FluentBitwarden.v1";
    public const int MaxPayloadLength = 1024 * 1024; // 1 MiB
}
