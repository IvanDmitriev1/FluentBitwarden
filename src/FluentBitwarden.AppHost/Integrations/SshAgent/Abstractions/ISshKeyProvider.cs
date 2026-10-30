using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;

internal interface ISshKeyProvider
{
    ValueTask<IReadOnlyList<SshAgentIdentity>> GetIdentitiesAsync();

    ValueTask<SshAgentSignature?> GetSignatureAsync(SshAgentSignRequest request, CancellationToken cancellationToken);
}
