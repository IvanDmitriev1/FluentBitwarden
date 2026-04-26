using FluentBitwarden.Application;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Shared.Ipc.Abstractions;

namespace FluentBitwarden.Modules.AppState.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppFirstRunService(
    ISettingsService settingsService,
    IDataInitializationService dataInitializationService) : IAppFirstRunService
{
    public async Task InitializeAsync()
    {
        var settings = settingsService.Get();
        if (settings.FirstRun)
            return;

        dataInitializationService.Initialize();
        await PasskeyPluginSetupService.EnsureRegisteredAsync();

        settingsService.Save(settings with { FirstRun = true });
    }
}
