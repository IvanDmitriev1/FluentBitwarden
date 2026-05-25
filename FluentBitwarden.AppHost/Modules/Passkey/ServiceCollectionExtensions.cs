using FluentBitwarden.Infrastructure.Ipc;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Modules.Passkey.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Passkey;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPasskeyModule(this IServiceCollection services)
    {
        services.AddIpcRequestHandler<PasskeyAssertionHandler, PasskeyGetAssertionRequest, PasskeyAssertionResponse>();

        return services;
    }
}
