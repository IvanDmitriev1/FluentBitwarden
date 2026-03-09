using BitwaredApi;
using BitwaredApi.Abstractions;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Services;
using FluentBitwarden.Services.UnlockServices;
using FluentBitwarden.Storage;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBitwaredPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<WindowHandleProvider>();
        services.AddSingleton<IWindowHandleProvider>(sp => sp.GetRequiredService<WindowHandleProvider>());
        services.AddSingleton<IUnlockSettingsPromptService, UnlockSettingsPromptService>();
        services.AddSingleton<IDeviceInfoProvider, LocalDeviceInfoProvider>();
        services.AddSingleton<ISessionStore, DpapiSessionStore>();
        services.AddSingleton<IVaultCache, SqliteVaultCache>();
        services.AddLocalUnlockServices();

        return services;
    }

    public static IServiceCollection AddBitwaredWorkflowServices(this IServiceCollection services)
    {
        services.AddSingleton<SessionCoordinator>();
        services.AddSingleton<IAccessTokenProvider>(sp => sp.GetRequiredService<SessionCoordinator>());
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IVaultService, VaultService>();
        services.AddSingleton<IBitwaredClient, BitwaredClient>();

        return services;
    }

    public static IServiceCollection AddLocalUnlockServices(this IServiceCollection services)
    {
        services.AddSingleton<WindowsHelloVerificationPrompt>();
        services.AddSingleton<LocalVaultUnlockStateRepository>();
        services.AddSingleton<ILocalVaultUnlocker, LocalVaultUnlocker>();
        services.AddSingleton<LocalVaultSessionUnlocker>();
        services.AddSingleton<IMasterPasswordUnlockService, MasterPasswordUnlockService>();
        services.AddSingleton<IWindowsHelloUnlockService, WindowsHelloUnlockService>();
        services.AddSingleton<IPinUnlockService, PinUnlockService>();

        return services;
    }
}
