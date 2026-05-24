namespace FluentBitwarden.Modules.SshAgent.Models;

[Flags]
public enum SshAgentSignatureFlags : uint
{
    None = 0,

    // Historical/reserved. Treat as unsupported.
    Reserved = 1,

    RsaSha2_256 = 2,
    RsaSha2_512 = 4
}
