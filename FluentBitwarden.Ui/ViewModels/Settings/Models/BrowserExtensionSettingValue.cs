using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Modules.BrowserExtension;

namespace FluentBitwarden.ViewModels.Settings.Models;

public sealed partial class BrowserExtensionSettingValue()
    : IntegrationSetupSettingValue(AppSettingKeys.Browser.ExtensionEnabledKey)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; private set; } = string.Empty;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    protected override Task EnableAsync()
    {
        BrowserExtensionSetupService.EnsureRegistered();
        return Task.CompletedTask;
    }

    protected override Task DisableAsync()
    {
        BrowserExtensionSetupService.Unregister();
        return Task.CompletedTask;
    }

    protected override void OnApplySucceeded(bool enabled)
    {
        StatusMessage = string.Empty;
    }

    protected override void OnApplyFailed(bool enabled, Exception exception)
    {
        StatusMessage = enabled
            ? "Could not register browser native messaging. See debug output."
            : "Could not unregister browser native messaging. See debug output.";
    }
}
