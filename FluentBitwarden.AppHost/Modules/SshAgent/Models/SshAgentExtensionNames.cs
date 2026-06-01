namespace FluentBitwarden.AppHost.Modules.SshAgent.Models;

internal static class SshAgentExtensionNames
{
    public static ReadOnlySpan<byte> Query => "query"u8;
    public static ReadOnlySpan<byte> SessionBindOpenSsh => "session-bind@openssh.com"u8;
}