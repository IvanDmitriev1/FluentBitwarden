using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Contracts.Settings;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal sealed class UiProcessLauncher(IHostApplicationLifetime applicationLifetime) : IUiProcessLauncher
{
    private const string UiExecutableName = "FluentBitwarden.Ui.exe";
    private const string UiProjectDirectoryName = "FluentBitwarden.Ui";

    private static readonly string PackageRoot = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
    private static readonly string ExecutablePath = Path.Combine(PackageRoot, UiProjectDirectoryName, UiExecutableName);

    private readonly Lock _lock = new();
    private Process? _uiProcess;

    public bool IsRunning
    {
        get
        {
            using var _ = _lock.EnterScope();
            return _uiProcess is not null;
        }
    }

    public void ActivateMainWindow()
        => StartProcess(string.Empty);

    public void ActivateOverlay()
        => StartProcess("--overlay");


    public void Activate()
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

    public void Exit()
    {
        bool isProcessRunning = IsRunning;

        CleanUpUiProcess();
        if (isProcessRunning)
        {
            StartProcess("--exit");
        }
    }

    private void StartProcess(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(ExecutablePath),
            UseShellExecute = false
        };

        var process = Process.Start(startInfo);
        ArgumentNullException.ThrowIfNull(process);

        using var _ = _lock.EnterScope();
        if (_uiProcess is not null)
        {
            process.Dispose();
            return;
        }

        process.EnableRaisingEvents = true;
        _uiProcess = process;
        _uiProcess.Exited += UiProcessOnExited;
    }

    private void UiProcessOnExited(object? sender, EventArgs e)
    {
        CleanUpUiProcess();

        if (SettingsStore.Instance.Get(AppSettingKeys.App.CloseToTrayKey))
            return;

        applicationLifetime.StopApplication();
    }

    private void CleanUpUiProcess()
    {
        using var _ = _lock.EnterScope();

        if (_uiProcess is null)
            return;

        _uiProcess.Exited -= UiProcessOnExited;
        _uiProcess.Dispose();
        _uiProcess = null;
    }
}
