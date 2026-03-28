using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Session;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationService, AuthenticationService>();

        return services;
    }
}