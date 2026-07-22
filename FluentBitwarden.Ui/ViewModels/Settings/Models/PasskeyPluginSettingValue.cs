using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Platform.Infrastructure.Integrations;

namespace FluentBitwarden.ViewModels.Settings.Models;

public sealed partial class PasskeyPluginSettingValue()
    : IntegrationSetupSettingValue(AppSettingKeys.Passkeys.PluginEnabledKey)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial bool IsSupported { get; private set; }

    [ObservableProperty]
    public partial string InfoBarStatus { get; private set; } = string.Empty;

    protected override bool CanApply => IsSupported;

    protected override void OnLoad()
    {
        IsSupported = PasskeyPluginSetupService.IsSupported();
        if (!IsSupported)
        {
            InfoBarStatus = "Passkey plugin integration requires Windows 11 24H2 or newer";
        }
    }

    protected override Task EnableAsync() => PasskeyPluginSetupService.EnsureRegisteredAsync();
    protected override Task DisableAsync() => PasskeyPluginSetupService.UnregisterAsync();
}
