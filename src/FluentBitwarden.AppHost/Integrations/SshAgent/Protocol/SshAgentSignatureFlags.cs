namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

[Flags]
internal enum SshAgentSignatureFlags : uint
{
    None = 0,
    RsaSha2256 = 2,
    RsaSha2512 = 4
}
