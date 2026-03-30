using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Security;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        if (TpmSecretProtector.IsAvailable)
            services.AddSingleton<ISecretProtector, TpmSecretProtector>();
        else
            services.AddSingleton<ISecretProtector, DpapiSecretProtector>();

        return services;
    }
}
