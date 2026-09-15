using AsyncAwaitBestPractices;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Platform.Infrastructure.Integrations;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppSetupService(IDataInitializationService dataInitializationService) : IAppSetupService
{
    public void Initialize()
    {
        dataInitializationService.Initialize();

        if (SettingsStore.Instance.Get(AppSettingKeys.Browser.ExtensionEnabledKey))
            BrowserExtensionSetupService.EnsureRegistered();

        var setupCompleted = SettingsStore.Instance.Get(AppSettingKeys.App.SetupCompletedKey);
        if (setupCompleted)
            return;

        InitialSetUp();
        SettingsStore.Instance.Set(AppSettingKeys.App.SetupCompletedKey, true);
    }

    private void InitialSetUp()
    {
        if (PasskeyPluginSetupService.IsSupported())
        {
            Task.Run(PasskeyPluginSetupService.EnsureRegisteredAsync).SafeFireAndForget();
            SettingsStore.Instance.Set(AppSettingKeys.Passkeys.PluginEnabledKey, true);
        }


    }
}
