using FluentBitwarden.Platform.Infrastructure.ProcessManager;
using System.Diagnostics;

namespace FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

internal sealed class CommandPaletteAppHostProcessManager() : ProcessManager(ExeName, ProcessDirectory), IAppHostProcessManager
{
    private const string ExeName = "FluentBitwarden.AppHost.exe";
    private const string ProcessDirectory = "FluentBitwarden.AppHost";

    public override void Activate()
    {
        using var activeProcess = GetActiveProcess(ExeName);
        if (activeProcess is not null)
            return;

        LunchProcess("--headless");
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
