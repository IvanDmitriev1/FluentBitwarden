namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal static class SshAgentDestinationResolver
{
    internal static string? TryResolve(
        in SshAgentSignRequest request,
        SshAgentSessionBinding? binding,
        Func<ReadOnlyMemory<byte>, string?> resolveHost)
    {
        if (!SshAgentAuthenticationPayload.TryParse(request.Data, request.PublicKeyBlob, out var auth))
            return null;

        if (binding is not { } verifiedBinding ||
            !auth.SessionId.Span.SequenceEqual(verifiedBinding.SessionId.Span) ||
            (auth.IsHostBound && !auth.HostKeyBlob.Span.SequenceEqual(verifiedBinding.HostKeyBlob.Span)))
        {
            return auth.Username;
        }

        string? host = resolveHost(verifiedBinding.HostKeyBlob);
        return host is null ? auth.Username : $"{auth.Username}@{host}";
    }
}
