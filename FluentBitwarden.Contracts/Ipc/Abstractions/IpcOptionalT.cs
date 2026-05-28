namespace FluentBitwarden.Contracts.Ipc.Abstractions;

[MemoryPackable]
public readonly partial record struct IpcOptional<T>(T? Value)
    where T : notnull;