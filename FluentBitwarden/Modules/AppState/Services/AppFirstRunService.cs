using FluentBitwarden.Application;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.AppState.Abstractions;

namespace FluentBitwarden.Modules.AppState.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppFirstRunService(
    ISettingsService settingsService,
    IDataInitializationService dataInitializationService) : IAppFirstRunService
{
    public async void Initialize()
    {
        await PasskeyPluginSetupService.UnregisterAsync();
        await PasskeyPluginSetupService.EnsureRegisteredAsync();

        var settings = settingsService.Get();
        if (settings.FirstRun)
            return;

        dataInitializationService.Initialize();

        settingsService.Save(settings with { FirstRun = true });
    }
}
