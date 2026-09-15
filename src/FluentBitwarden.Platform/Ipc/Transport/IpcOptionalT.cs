namespace FluentBitwarden.Platform.Ipc.Transport;

[MemoryPackable(SerializeLayout.Explicit)]
internal readonly partial record struct IpcOptional<T>([property: MemoryPackOrder(0)] T Value);