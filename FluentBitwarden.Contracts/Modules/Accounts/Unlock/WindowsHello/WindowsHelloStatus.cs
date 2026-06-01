namespace FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

[MemoryPackable]
public readonly partial record struct WindowsHelloStatus(
    bool IsSupported,
    bool IsEnabled);