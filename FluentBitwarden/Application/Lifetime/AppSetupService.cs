using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.AppState;

namespace FluentBitwarden.Application.Lifetime;

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
