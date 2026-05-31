using FluentBitwarden.AppHost.Modules.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Modules.SshAgent.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.SshAgent;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSshAgent(this IServiceCollection services)
    {
        services.AddHostedService<SshAgentServer>();
        services.AddSingleton<ISshKeyProvider, SshKeyProvider>();
        services.AddSingleton<ISshUserActionPrompt, TmpISshUserActionPrompt>();

        return services;
    }
}
