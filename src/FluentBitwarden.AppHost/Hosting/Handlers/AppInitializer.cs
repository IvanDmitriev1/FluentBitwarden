using AsyncAwaitBestPractices;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Platform.Infrastructure.Integrations;
using FluentBitwarden.Platform.Settings;

namespace FluentBitwarden.AppHost.Hosting.Handlers;

internal sealed class AppInitializer(IDatabaseInitializationService databaseInitializationService)
{
    public void Initialize()
    {
        databaseInitializationService.Initialize();

        if (SettingsStore.Instance.Get(AppSettingKeys.App.SetupCompletedKey))
        {
            return;
        }

        if (PasskeyPluginSetupService.IsSupported())
        {
            PasskeyPluginSetupService.EnsureRegisteredAsync().SafeFireAndForget();

            SettingsStore.Instance.Set(
                AppSettingKeys.Passkeys.PluginEnabledKey,
                true);
        }

        SettingsStore.Instance.Set(
            AppSettingKeys.App.SetupCompletedKey,
            true);
    }
}
