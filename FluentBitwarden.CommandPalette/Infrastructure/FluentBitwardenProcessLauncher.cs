using System.ComponentModel;
using System.Diagnostics;
using FluentBitwarden.Platform.Infrastructure.Extensions;

namespace FluentBitwarden.CommandPalette.Infrastructure;

internal static class FluentBitwardenProcessLauncher
{
    private const string AppHostProjectDirectory = "FluentBitwarden.AppHost";
    private const string AppHostExecutable = "FluentBitwarden.AppHost.exe";
    private const string UiProjectDirectory = "FluentBitwarden.Ui";
    private const string UiExecutable = "FluentBitwarden.Ui.exe";

    public static bool EnsureAppHostRunning()
    {
        var processName = Path.GetFileNameWithoutExtension(AppHostExecutable);
        var processes = Process.GetProcessesByName(processName);
        try
        {
            if (processes.Length > 0)
                return true;
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }

        return StartPackagedProcess(AppHostProjectDirectory, AppHostExecutable, "--headless");
    }

    public static bool OpenUnlockOverlay() =>
        StartPackagedProcess(UiProjectDirectory, UiExecutable, "--overlay");

    private static bool StartPackagedProcess(
        string projectDirectory,
        string executableName,
        string arguments)
    {
        var executablePath = Path.Combine(
            PackageHelper.AppBasePath,
            projectDirectory,
            executableName);

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
            });

            return process is not null;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or Win32Exception
                or IOException)
        {
            return false;
        }
    }
}
