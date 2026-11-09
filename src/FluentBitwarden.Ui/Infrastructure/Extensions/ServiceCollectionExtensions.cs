using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Infrastructure.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostedSingleton<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation,
        TService>(
        this IServiceCollection services)
        where TImplementation : class, TService, IHostedService
        where TService : class
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<TService>(static serviceProvider =>
            serviceProvider.GetRequiredService<TImplementation>());
        services.AddHostedService(static serviceProvider =>
            serviceProvider.GetRequiredService<TImplementation>());

        return services;
    }
}
