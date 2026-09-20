using FluentBitwarden.Platform.Infrastructure.ProcessManager;

namespace FluentBitwarden.AppHost.Infrastructure.Processes;

internal interface IUiProcessLauncher : IProcessManager
{
    void ActivateMainWindow();
    void ActivateOverlay();
}
