using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Internal;

internal static class SshAgentSignatureFlagsExtensions
{
    private const SshAgentSignatureFlags Supported =
        SshAgentSignatureFlags.RsaSha2_256 |
        SshAgentSignatureFlags.RsaSha2_512;

    public static bool HasSupportedSignatures(this SshAgentSignatureFlags flags)
    {
        return (flags & ~Supported) == 0 && flags != Supported;
    }
}