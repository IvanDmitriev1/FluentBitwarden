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

        _ = Task.Run(PasskeyPluginSetupService.EnsureRegisteredAsync);
        dataInitializationService.Initialize();

        SettingsStore.Instance.Set(AppSettingKeys.App.SetupCompletedKey, true);
    }
}
