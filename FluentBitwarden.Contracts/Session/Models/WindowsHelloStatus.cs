namespace FluentBitwarden.Contracts.Session.Models;

public sealed record WindowsHelloStatus(
    bool IsSupported,
    bool IsEnabled);