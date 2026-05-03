namespace FluentBitwarden.Modules.SshAgent.Models;

[Flags]
public enum SshAgentSignFlags : uint
{
    None = 0,
    ReservedHistorical = 0x00000001,
    RsaSha2256 = 0x00000002,
    RsaSha2512 = 0x00000004
}
