using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.ProcessManager;
using FluentBitwarden.Platform.Settings;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Infrastructure.Processes;

public sealed class UiProcessLauncher(IHostApplicationLifetime applicationLifetime) : ProcessManager(ExeName, ExeDirectoryName), IUiProcessLauncher
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
