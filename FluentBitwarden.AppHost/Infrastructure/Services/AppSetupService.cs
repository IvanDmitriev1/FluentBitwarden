using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.Contracts.AppState;
using FluentBitwarden.Contracts.Shared;
using FluentBitwarden.Data.Abstractions;

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
