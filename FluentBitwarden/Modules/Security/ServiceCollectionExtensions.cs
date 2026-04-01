using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Services.Unlock;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Security;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        services.AddSingleton<IUnlockService, UnlockServices>();

        services.AddSingleton<MasterPasswordUnlockStrategy>();

        return services;
    }
}
