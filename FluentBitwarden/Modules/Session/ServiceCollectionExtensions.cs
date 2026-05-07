using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Session;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {

        return services;
    }
}