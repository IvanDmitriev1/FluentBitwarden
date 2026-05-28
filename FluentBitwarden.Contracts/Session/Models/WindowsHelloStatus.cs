namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable]
public readonly partial record struct WindowsHelloStatus(
    bool IsSupported,
    bool IsEnabled);