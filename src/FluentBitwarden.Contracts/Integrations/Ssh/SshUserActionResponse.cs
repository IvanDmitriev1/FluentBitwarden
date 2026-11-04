namespace FluentBitwarden.Contracts.Integrations.Ssh;

[MemoryPackable]
public readonly partial record struct SshUserActionResponse(bool Approved);
