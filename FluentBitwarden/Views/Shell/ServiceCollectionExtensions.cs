using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Views.Shell.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Views.Shell;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShellServices(this IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<ShellPage>();
        services.AddSingleton<IMessenger>(StrongReferenceMessenger.Default);

        services.AddShellService<INavigationService, NavigationService>();

        return services;
    }

    private static void AddShellService<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<TInterface>(static sp => sp.GetRequiredService<TImplementation>());
    }
}
