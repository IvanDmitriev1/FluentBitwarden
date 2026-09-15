using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Platform.Infrastructure.ProcessManager;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal sealed class AppHostUiProcessManager(IHostApplicationLifetime applicationLifetime) : ProcessManager(ExeName, ExeDirectoryName), IUiProcessLauncher
{
    private const string ExeName = "FluentBitwarden.Ui.exe";
    private const string ExeDirectoryName = "FluentBitwarden.Ui";

    public void ActivateMainWindow() => LunchProcess(string.Empty);
    public void ActivateOverlay() => LunchProcess("--overlay");

    public override void Activate()
    {
        if (IsRunning)
        {
            ActivateMainWindow();
        }
        else
        {
            ActivateOverlay();
        }
    }

    protected override void OnProcessExited()
    {
        base.OnProcessExited();

        if (SettingsStore.Instance.Get(AppSettingKeys.App.CloseToTrayKey))
            return;

        applicationLifetime.StopApplication();
    }
}
