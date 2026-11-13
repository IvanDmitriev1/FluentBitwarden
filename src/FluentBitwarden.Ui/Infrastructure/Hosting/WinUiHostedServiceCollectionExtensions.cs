using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Infrastructure.Hosting;

internal static class WinUiHostedServiceCollectionExtensions
{
    public static IServiceCollection AddWinUiHostedService<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication>(
        this IServiceCollection services,
        Func<IServiceProvider, TApplication> createApplication)
        where TApplication : Microsoft.UI.Xaml.Application
    {
        ArgumentNullException.ThrowIfNull(createApplication);

        services.AddSingleton<WinUiHostedService<TApplication>>(serviceProvider => new(
            () => createApplication(serviceProvider),
            serviceProvider.GetRequiredService<IHostApplicationLifetime>(),
            serviceProvider.GetRequiredService<ILogger<WinUiHostedService<TApplication>>>()));
        services.AddSingleton<IHostedService>(static serviceProvider =>
            serviceProvider.GetRequiredService<WinUiHostedService<TApplication>>());

        return services;
    }
}
