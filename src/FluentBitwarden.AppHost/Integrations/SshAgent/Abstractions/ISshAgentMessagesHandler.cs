using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;

internal interface ISshAgentMessagesHandler
{
    Task<SshAgentMessage> HandleAsync(SshAgentMessage message, CancellationToken cancellationToken);
}
