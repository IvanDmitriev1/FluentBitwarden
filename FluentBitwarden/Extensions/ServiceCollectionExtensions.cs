using BitwaredApi;
using BitwaredApi.Abstractions;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Services;
using FluentBitwarden.Services.Storage;
using FluentBitwarden.Services.UnlockServices;
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
        services.AddSingleton<LocalDeviceInfoProvider>();
        services.AddSingleton<ISessionStore, SessionStore>();
        services.AddSingleton<IVaultCache, SqliteVaultCache>();
        services.AddLocalUnlockServices();

        return services;
    }

    public static IServiceCollection AddBitwaredWorkflowServices(this IServiceCollection services)
    {
        services.AddSingleton<SessionManager>();
        services.AddSingleton<ISessionManager>(sp => sp.GetRequiredService<SessionManager>());
        services.AddSingleton<IVaultService, VaultService>();

        return services;
    }

    public static IServiceCollection AddLocalUnlockServices(this IServiceCollection services)
    {
        services.AddSingleton<WindowsHelloVerificationPrompt>();
        services.AddSingleton<ILocalVaultStateStore, LocalVaultStateStore>();
        services.AddSingleton<ILocalVaultKeyManager, LocalVaultKeyManager>();
        services.AddSingleton<IMasterPasswordUnlockService, MasterPasswordUnlockService>();
        services.AddSingleton<IWindowsHelloUnlockService, WindowsHelloUnlockService>();
        services.AddSingleton<IPinUnlockService, PinUnlockService>();

        return services;
    }
}
