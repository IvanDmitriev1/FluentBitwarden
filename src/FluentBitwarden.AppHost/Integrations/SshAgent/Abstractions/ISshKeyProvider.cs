using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    IReadOnlyList<SshAgentIdentity> GetIdentities();

    ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken);
}
