using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using FluentBitwarden.Contracts.Integrations.Ssh;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Integrations.SshAgent;

public static class SshAgentServiceCollectionExtensions
{
    public static void AddSshAgentIntegration(this IServiceCollection services)
    {
        services.AddSingleton<ISshUserActionDialogClient, SshUserActionDialogClient>();
        services.AddScoped<ISshAgentMessagesHandler, SshAgentMessagesHandler>();
        services.AddScoped<ISshKeyProvider, SshKeyProvider>();

        services.AddHostedService<SshAgentServer>();
    }
}
