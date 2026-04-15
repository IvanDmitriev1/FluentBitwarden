using System.Diagnostics;
using Windows.ApplicationModel;

namespace FluentBitwarden.Application;

public static class PasskeyPluginSetupService
{
    private const int Windows11_24H2Build = 26100;

    public static Task EnsureRegisteredAsync()
        => RunPackagedComServerAsync("--register-plugin");

    public static Task UnregisterAsync()
        => RunPackagedComServerAsync("--unregister-plugin");

    private static async Task RunPackagedComServerAsync(string arguments)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, Windows11_24H2Build))
        {
            return;
        }

        string executablePath = Path.Combine(
            Package.Current.InstalledLocation.Path,
            "FluentBitwarden.ComServer",
            "FluentBitwarden.ComServer.exe");

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "FluentBitwarden.ComServer.exe was not found in the installed package.",
                executablePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(arguments);

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new InvalidOperationException(
                $"Failed to start '{executablePath}'.");
        }
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"FluentBitwarden.ComServer exited with code {process.ExitCode} for '{arguments}'.");
        }
    }
}
