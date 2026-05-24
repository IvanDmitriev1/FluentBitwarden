using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Abstractions.Dialog;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationService, NotificationService>();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(static sp => sp.GetRequiredService<NavigationService>());

        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<IWindowHandleProvider, MainWindowHandleProvider>();

        return services;
    }
}
