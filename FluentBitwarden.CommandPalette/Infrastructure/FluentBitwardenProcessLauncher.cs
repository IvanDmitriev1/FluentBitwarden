using System.Diagnostics;
using FluentBitwarden.Platform.Infrastructure.Extensions;

namespace FluentBitwarden.CommandPalette.Infrastructure;

internal static class FluentBitwardenProcessLauncher
{
    private const string AppHostProjectDirectory = "FluentBitwarden.AppHost";
    private const string AppHostExecutable = "FluentBitwarden.AppHost.exe";
    private const string UiProjectDirectory = "FluentBitwarden.Ui";
    private const string UiExecutable = "FluentBitwarden.Ui.exe";

    public static void EnsureAppHostRunning()
    {
        var processName = Path.GetFileNameWithoutExtension(AppHostExecutable);
        var processes = Process.GetProcessesByName(processName);
        Array.ForEach(processes, static p => p.Dispose());

        if (processes.Length > 0)
            return;

        StartPackagedProcess(AppHostProjectDirectory, AppHostExecutable, "--headless");
    }

    public static void OpenUnlockOverlay() => StartPackagedProcess(UiProjectDirectory, UiExecutable, "--overlay");

    private static void StartPackagedProcess(string projectDirectory, string executableName, string arguments)
    {
        var executablePath = Path.Combine(PackageHelper.AppBasePath, projectDirectory, executableName);

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false,
        });
    }
}
