using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Settings;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using FluentBitwarden.Contracts.Modules.AppState;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppSetupService(IDataInitializationService dataInitializationService) : IAppSetupService
{
    public void Initialize()
    {
        var setupCompleted = SettingsStore.Instance.Get(AppSettingKeys.App.SetupCompletedKey);
        if (setupCompleted)
            return;

        dataInitializationService.Initialize();

        if (PasskeyPluginSetupService.IsSupported())
        {
            _ = Task.Run(PasskeyPluginSetupService.EnsureRegisteredAsync);
            SettingsStore.Instance.Set(AppSettingKeys.Passkeys.PluginEnabledKey, true);
        }

        SettingsStore.Instance.Set(AppSettingKeys.App.SetupCompletedKey, true);
    }
}
