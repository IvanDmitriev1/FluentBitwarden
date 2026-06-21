using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Passkey;

internal static class PasskeyServiceCollectionExtensions
{
    public static IServiceCollection AddPasskeyServices(this IServiceCollection services)
    {
        services.AddSingleton<PasskeyAssertionService>();
        return services;
    }
}
