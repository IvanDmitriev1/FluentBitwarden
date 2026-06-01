namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

[MemoryPackable(SerializeLayout.Explicit)]
public readonly partial record struct IpcOptional<T>([property: MemoryPackOrder(0)] T Value);