using FluentBitwarden.Contracts.Infrastructure.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Passkey;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPasskeyModule(this IServiceCollection services)
    {
        services.AddIpcRequestHandler<PasskeyClientHandler>();

        return services;
    }
}
