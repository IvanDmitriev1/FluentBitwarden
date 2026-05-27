using FluentBitwarden.Modules.Passkey.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Passkey;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPasskeyModule(this IServiceCollection services)
    {
        services.MapPasskeyIpc();

        return services;
    }
}
