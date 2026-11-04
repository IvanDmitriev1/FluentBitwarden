using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    ValueTask<IReadOnlyList<SshAgentIdentity>> GetIdentitiesAsync(CancellationToken cancellationToken);

    ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken);
}
