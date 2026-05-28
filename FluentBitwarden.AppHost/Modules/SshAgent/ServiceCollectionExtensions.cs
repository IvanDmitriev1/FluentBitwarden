using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.SshAgent;

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
