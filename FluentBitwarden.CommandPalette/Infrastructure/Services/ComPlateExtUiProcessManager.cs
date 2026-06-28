using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Platform.Infrastructure.ProcessManager;
using System.Diagnostics;

namespace FluentBitwarden.CommandPalette.Infrastructure.Services;

internal sealed class ComPlateExtUiProcessManager() : ProcessManager(ExeName, ProcessDirectoryName),  IUiProcessManager
{
    private const string ExeName = "FluentBitwarden.Ui.exe";
    private const string ProcessDirectoryName = "FluentBitwarden.Ui";

    public void OpenItem(CipherId cipherId) => LunchProcess($"--open-item {cipherId}");

    public override void Activate()
    {
        if (GetActiveProcess(ExeName) is { } activeProcess)
        {
            activeProcess.CloseMainWindow();
            activeProcess.Dispose();
        }

        LunchProcess("--overlay");
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

