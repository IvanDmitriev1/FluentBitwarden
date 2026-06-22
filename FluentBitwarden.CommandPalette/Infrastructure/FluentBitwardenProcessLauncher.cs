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
        if (GetActiveProcess(AppHostExecutable) is { } activeProcess)
        {
            activeProcess.Dispose();
            return;
        }

        using var _ = StartPackagedProcess(AppHostProjectDirectory, AppHostExecutable, "--headless");
    }

    public static Process OpenUnlockOverlay()
    {
        if (GetActiveProcess(UiExecutable) is { } activeProcess)
        {
            activeProcess.CloseMainWindow();
            activeProcess.Dispose();
        }
        
        return StartPackagedProcess(UiProjectDirectory, UiExecutable, "--overlay");
    }

    private static Process StartPackagedProcess(string projectDirectory, string executableName, string arguments)
    {
        var executablePath = Path.Combine(PackageHelper.AppBasePath, projectDirectory, executableName);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false,
        });

        ArgumentNullException.ThrowIfNull(process);
        return process;
    }

    private static Process? GetActiveProcess(string processName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(processName);
        var processes = Process.GetProcessesByName(nameWithoutExtension);

        return processes.Length switch
        {
            0 => null,
            1 => processes[0],
            _ => throw new InvalidOperationException($"Multiple processes found with name '{processName}'")
        };
    }
}
