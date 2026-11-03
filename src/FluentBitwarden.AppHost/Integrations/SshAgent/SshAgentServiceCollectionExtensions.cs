using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Integrations.SshAgent;

public static class SshAgentServiceCollectionExtensions
{
    public static void AddSshAgentIntegration(this IServiceCollection services)
    {
        services.AddScoped<ISshAgentMessagesHandler, SshAgentMessagesHandler>();
        services.AddScoped<ISshKeyProvider, SshKeyProvider>();

        services.AddHostedService<SshAgentServer>();
    }
}
