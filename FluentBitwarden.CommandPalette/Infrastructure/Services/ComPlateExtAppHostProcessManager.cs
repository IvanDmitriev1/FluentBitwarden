using FluentBitwarden.Platform.Infrastructure.ProcessManager;

namespace FluentBitwarden.CommandPalette.Infrastructure.Services;

internal sealed class ComPlateExtAppHostProcessManager() : ProcessManager(ExeName, ProcessDirectory), IAppHostProcessManager
{
    private const string ExeName = "FluentBitwarden.AppHost.exe";
    private const string ProcessDirectory = "FluentBitwarden.AppHost";
}