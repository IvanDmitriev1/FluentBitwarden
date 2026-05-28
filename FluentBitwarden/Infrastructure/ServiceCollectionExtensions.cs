using FluentBitwarden.AppHost.Infrastructure.Services;
using FluentBitwarden.Contracts.Ipc;
using FluentBitwarden.Contracts.Session.Abstractions;
using FluentBitwarden.Contracts.Vault.Abstractions;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Abstractions.Dialog;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Infrastructure.IpcClientsImplementations;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(static sp => sp.GetRequiredService<NavigationService>());

        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<ISiteIconCache, SiteIconCache>();


        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddSingleton<IAccountSessionManagerClient, RemoteAccountSessionManagerClient>();
        services.AddSingleton<IWindowsHelloUnlockClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultManagerClient, RemoteVaultManagerClient>();

        return services;
    }
}
