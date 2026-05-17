namespace FluentBitwarden.Infrastructure.Ipc;

public static class IpcConstants
{
    public const UInt16 ProtocolVersion = 2;
    public const string PipeName = @"LOCAL\FluentBitwarden.v2";
    public const int MaxPayloadLength = 1024 * 1024; // 1 MiB
}
