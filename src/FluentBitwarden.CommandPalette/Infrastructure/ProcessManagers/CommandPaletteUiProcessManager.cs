using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Platform.Infrastructure.ProcessManager;

namespace FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;

internal sealed class CommandPaletteUiProcessManager() : ProcessManager(ExeName, ProcessDirectoryName), IUiProcessManager
{
    private const string ExeName = "FluentBitwarden.Ui.exe";
    private const string ProcessDirectoryName = "FluentBitwarden.Ui";

    public void OpenItem(CipherId cipherId) => LunchProcess($"--open-item {cipherId}");
}

