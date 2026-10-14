using FluentBitwarden.Platform.Infrastructure.ProcessManager;

namespace FluentBitwarden.AppHost.Infrastructure.Processes;

public interface IUiProcessLauncher : IProcessManager
{
    void ActivateMainWindow();
    void ActivateOverlay();
}
