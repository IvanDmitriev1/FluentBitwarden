namespace FluentBitwarden.Platform.Ipc.Transport;

[MemoryPackable]
public readonly partial struct IpcVoid
{
    public static IpcVoid Value => default;
}